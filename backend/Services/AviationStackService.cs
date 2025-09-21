using System.Text.Json;
using System.Text.Json.Serialization;
using Polly;
using Polly.CircuitBreaker;

namespace AirlineSimulationApi.Services;

public class AviationStackService : IFlightDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AviationStackService> _logger;
    private readonly string _apiKey;
    private readonly IAsyncPolicy _retryPolicy;

    public AviationStackService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<AviationStackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["AviationStack:ApiKey"] ?? throw new InvalidOperationException("AviationStack API key not configured");
        
        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<FlightDataServiceException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry attempt {RetryCount} for AviationStack API call after {Delay}ms", 
                        retryCount, timespan.TotalMilliseconds);
                });
    }

    public async Task<IEnumerable<ExternalFlightData>> GetFlightDataAsync(string airportCode)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var url = $"flights?access_key={_apiKey}&dep_iata={airportCode}&limit=100";
                _logger.LogDebug("Calling AviationStack API: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AviationStack API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new FlightDataServiceException($"AviationStack API returned {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<AviationStackResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Data == null)
                {
                    _logger.LogWarning("AviationStack API returned null or empty data");
                    return Enumerable.Empty<ExternalFlightData>();
                }

                return apiResponse.Data.Select(MapToExternalFlightData);
            });
        }
        catch (Exception ex) when (!(ex is FlightDataServiceException))
        {
            _logger.LogError(ex, "Unexpected error calling AviationStack API");
            throw new FlightDataServiceException("Failed to retrieve flight data from AviationStack", ex);
        }
    }

    public async Task<ExternalFlightData?> GetFlightDetailsAsync(string flightNumber, DateTime date)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var url = $"flights?access_key={_apiKey}&flight_iata={flightNumber}&limit=1";
                _logger.LogDebug("Calling AviationStack API for flight details: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AviationStack API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new FlightDataServiceException($"AviationStack API returned {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<AviationStackResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var flight = apiResponse?.Data?.FirstOrDefault();
                return flight != null ? MapToExternalFlightData(flight) : null;
            });
        }
        catch (Exception ex) when (!(ex is FlightDataServiceException))
        {
            _logger.LogError(ex, "Unexpected error calling AviationStack API for flight details");
            throw new FlightDataServiceException("Failed to retrieve flight details from AviationStack", ex);
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"flights?access_key={_apiKey}&limit=1");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AviationStack service availability check failed");
            return false;
        }
    }

    private static ExternalFlightData MapToExternalFlightData(AviationStackFlight flight)
    {
        return new ExternalFlightData
        {
            FlightNumber = flight.Flight?.Iata ?? string.Empty,
            Airline = flight.Airline?.Name ?? string.Empty,
            AirlineIata = flight.Airline?.Iata ?? string.Empty,
            OriginAirport = flight.Departure?.Iata ?? string.Empty,
            DestinationAirport = flight.Arrival?.Iata ?? string.Empty,
            ScheduledDeparture = ParseDateTime(flight.Departure?.Scheduled),
            EstimatedDeparture = ParseDateTime(flight.Departure?.Estimated),
            ScheduledArrival = ParseDateTime(flight.Arrival?.Scheduled),
            EstimatedArrival = ParseDateTime(flight.Arrival?.Estimated),
            Status = flight.FlightStatus ?? "Unknown",
            Gate = flight.Departure?.Gate,
            Terminal = flight.Departure?.Terminal,
            Aircraft = flight.Aircraft?.Registration
        };
    }

    private static DateTime ParseDateTime(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return DateTime.MinValue;

        return DateTime.TryParse(dateTimeString, out var result) ? result : DateTime.MinValue;
    }
}

// AviationStack API response models
public class AviationStackResponse
{
    public AviationStackFlight[]? Data { get; set; }
}

public class AviationStackFlight
{
    [JsonPropertyName("flight_date")]
    public string? FlightDate { get; set; }
    
    [JsonPropertyName("flight_status")]
    public string? FlightStatus { get; set; }
    
    public AviationStackFlightInfo? Flight { get; set; }
    public AviationStackAirline? Airline { get; set; }
    public AviationStackAirport? Departure { get; set; }
    public AviationStackAirport? Arrival { get; set; }
    public AviationStackAircraft? Aircraft { get; set; }
}

public class AviationStackFlightInfo
{
    public string? Number { get; set; }
    public string? Iata { get; set; }
    public string? Icao { get; set; }
}

public class AviationStackAirline
{
    public string? Name { get; set; }
    public string? Iata { get; set; }
    public string? Icao { get; set; }
}

public class AviationStackAirport
{
    public string? Airport { get; set; }
    public string? Timezone { get; set; }
    public string? Iata { get; set; }
    public string? Icao { get; set; }
    public string? Terminal { get; set; }
    public string? Gate { get; set; }
    public string? Scheduled { get; set; }
    public string? Estimated { get; set; }
    public string? Actual { get; set; }
}

public class AviationStackAircraft
{
    public string? Registration { get; set; }
    public string? Iata { get; set; }
    public string? Icao { get; set; }
    public string? Icao24 { get; set; }
}