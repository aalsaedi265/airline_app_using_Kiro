using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public interface IFlightService
{
    Task<IEnumerable<Flight>> GetFlightBoardAsync(string airportCode);
    Task<Flight?> GetFlightDetailsAsync(string flightNumber, DateTime date);
    Task<WeatherInfo?> GetWeatherAsync(string airportCode);
    Task UpdateFlightStatusAsync(string flightNumber, FlightStatus status);
    Task UpdateFlightGateAsync(string flightNumber, string? newGate, string? terminal = null);
    Task UpdateFlightDelayAsync(string flightNumber, int delayMinutes, string? reason = null);
}

public class WeatherInfo
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public double Visibility { get; set; }
    public double WindSpeed { get; set; }
    public string WindDirection { get; set; } = string.Empty;
}

