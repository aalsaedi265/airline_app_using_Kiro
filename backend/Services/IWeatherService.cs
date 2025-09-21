namespace AirlineSimulationApi.Services;

public interface IWeatherService
{
    Task<WeatherData?> GetWeatherAsync(string airportCode);
    Task<WeatherData?> GetWeatherByCityAsync(string cityName);
    Task<bool> IsServiceAvailableAsync();
}

public class WeatherData
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double TemperatureFahrenheit => (Temperature * 9 / 5) + 32;
    public string Conditions { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public double Visibility { get; set; }
    public double WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public string WindDirectionText { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class WeatherServiceException : Exception
{
    public WeatherServiceException(string message) : base(message) { }
    public WeatherServiceException(string message, Exception innerException) : base(message, innerException) { }
}