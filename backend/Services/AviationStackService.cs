using System.Text.Json;
using System.Text.Json.Serialization;

namespace AirlineSimulationApi.Services;

public class AviationStackService : IFlightDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AviationStackService> _logger;
    private readonly string? _apiKey;

    public AviationStackService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<AviationStackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("AVIATION_STACK_API_KEY") ?? 
                  configuration["AviationStack:ApiKey"];
        
        if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("your-aviation-stack-api-key-here"))
        {
            _logger.LogWarning("AviationStack API key not configured or using placeholder value. External flight data will not be available.");
            _apiKey = null; // Set to null to indicate API is not available
        }
    }

    public async Task<IEnumerable<ExternalFlightData>> GetFlightDataAsync(string airportCode)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("AviationStack API key not configured, returning empty flight data for airport {AirportCode}", airportCode);
            return Enumerable.Empty<ExternalFlightData>();
        }

        try
        {
            var url = $"flights?access_key={_apiKey}&dep_iata={airportCode}&limit=100";
            _logger.LogDebug("Calling AviationStack API: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Handle rate limiting specifically
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("AviationStack API rate limit reached: {Content}", errorContent);
                    throw new FlightDataServiceException($"AviationStack API rate limit reached");
                }

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
        }
        catch (Exception ex) when (!(ex is FlightDataServiceException))
        {
            _logger.LogError(ex, "Unexpected error calling AviationStack API");
            throw new FlightDataServiceException("Failed to retrieve flight data from AviationStack", ex);
        }
    }

    public async Task<ExternalFlightData?> GetFlightDetailsAsync(string flightNumber, DateTime date)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("AviationStack API key not configured, returning null for flight details {FlightNumber}", flightNumber);
            return null;
        }

        try
        {
            var url = $"flights?access_key={_apiKey}&flight_iata={flightNumber}&limit=1";
            _logger.LogDebug("Calling AviationStack API for flight details: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Handle rate limiting specifically
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("AviationStack API rate limit reached: {Content}", errorContent);
                    throw new FlightDataServiceException($"AviationStack API rate limit reached");
                }

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
        }
        catch (Exception ex) when (!(ex is FlightDataServiceException))
        {
            _logger.LogError(ex, "Unexpected error calling AviationStack API for flight details");
            throw new FlightDataServiceException("Failed to retrieve flight details from AviationStack", ex);
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("AviationStack API key not configured, service not available");
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync($"flights?access_key={_apiKey}&limit=1");

            // If we get a rate limit error, the service is technically available but we can't use it
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("AviationStack service available but rate limited");
                return false;
            }

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