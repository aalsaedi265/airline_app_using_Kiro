using System.Text.Json;
using System.Text.Json.Serialization;

namespace AirlineSimulationApi.Services;

public class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenWeatherMapService> _logger;
    private readonly string _apiKey;

    // Airport code to city mapping for weather lookup
    private static readonly Dictionary<string, string> AirportCityMap = new()
    {
        { "ORD", "Chicago" },
        { "LAX", "Los Angeles" },
        { "JFK", "New York" },
        { "LGA", "New York" },
        { "EWR", "Newark" },
        { "DFW", "Dallas" },
        { "DEN", "Denver" },
        { "ATL", "Atlanta" },
        { "PHX", "Phoenix" },
        { "SEA", "Seattle" },
        { "SFO", "San Francisco" },
        { "LAS", "Las Vegas" },
        { "MCO", "Orlando" },
        { "MIA", "Miami" },
        { "BOS", "Boston" },
        { "MSP", "Minneapolis" },
        { "DTW", "Detroit" },
        { "PHL", "Philadelphia" },
        { "CLT", "Charlotte" },
        { "IAH", "Houston" }
    };

    public OpenWeatherMapService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<OpenWeatherMapService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY") ?? 
                  configuration["OpenWeatherMap:ApiKey"] ?? 
                  throw new InvalidOperationException("OpenWeatherMap API key not configured");
    }

    public async Task<WeatherData?> GetWeatherAsync(string airportCode)
    {
        if (!AirportCityMap.TryGetValue(airportCode.ToUpper(), out var cityName))
        {
            _logger.LogWarning("Airport code {AirportCode} not found in city mapping", airportCode);
            return null;
        }

        return await GetWeatherByCityAsync(cityName);
    }

    public async Task<WeatherData?> GetWeatherByCityAsync(string cityName)
    {
        try
        {
            var url = $"weather?q={Uri.EscapeDataString(cityName)}&appid={_apiKey}&units=metric";
            _logger.LogDebug("Calling OpenWeatherMap API: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenWeatherMap API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("City {CityName} not found in OpenWeatherMap", cityName);
                    return null;
                }
                
                throw new WeatherServiceException($"OpenWeatherMap API returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var weatherResponse = JsonSerializer.Deserialize<OpenWeatherMapResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return weatherResponse != null ? MapToWeatherData(weatherResponse) : null;
        }
        catch (Exception ex) when (!(ex is WeatherServiceException))
        {
            _logger.LogError(ex, "Unexpected error calling OpenWeatherMap API");
            throw new WeatherServiceException("Failed to retrieve weather data from OpenWeatherMap", ex);
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"weather?q=Chicago&appid={_apiKey}&units=metric");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenWeatherMap service availability check failed");
            return false;
        }
    }

    private static WeatherData MapToWeatherData(OpenWeatherMapResponse response)
    {
        var weather = response.Weather?.FirstOrDefault();
        var windDirection = GetWindDirection(response.Wind?.Deg ?? 0);

        return new WeatherData
        {
            Location = response.Name ?? string.Empty,
            Temperature = response.Main?.Temp ?? 0,
            Conditions = weather?.Main ?? string.Empty,
            Description = weather?.Description ?? string.Empty,
            Humidity = response.Main?.Humidity ?? 0,
            Pressure = response.Main?.Pressure ?? 0,
            Visibility = (response.Visibility ?? 0) / 1000.0, // Convert meters to kilometers
            WindSpeed = response.Wind?.Speed ?? 0,
            WindDirection = response.Wind?.Deg ?? 0,
            WindDirectionText = windDirection,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static string GetWindDirection(int degrees)
    {
        var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        var index = (int)Math.Round(degrees / 22.5) % 16;
        return directions[index];
    }
}

// OpenWeatherMap API response models
public class OpenWeatherMapResponse
{
    public OpenWeatherMapWeather[]? Weather { get; set; }
    public OpenWeatherMapMain? Main { get; set; }
    public int? Visibility { get; set; }
    public OpenWeatherMapWind? Wind { get; set; }
    public string? Name { get; set; }
}

public class OpenWeatherMapWeather
{
    public string? Main { get; set; }
    public string? Description { get; set; }
}

public class OpenWeatherMapMain
{
    public double Temp { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
}

public class OpenWeatherMapWind
{
    public double Speed { get; set; }
    public int Deg { get; set; }
}