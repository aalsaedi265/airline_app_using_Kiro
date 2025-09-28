namespace AirlineSimulationApi.Services;

public interface IWeatherService
{
    Task<WeatherData?> GetWeatherAsync(string airportCode);
    Task<WeatherData?> GetWeatherByCityAsync(string cityName);
    Task<bool> IsServiceAvailableAsync();
}


public class WeatherServiceException : Exception
{
    public WeatherServiceException(string message) : base(message) { }
    public WeatherServiceException(string message, Exception innerException) : base(message, innerException) { }
}