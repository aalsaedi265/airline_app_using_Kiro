using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineSimulationApi.Services;

public class FlightService : IFlightService
{
    private readonly ApplicationDbContext _context;
    private readonly IFlightDataService _flightDataService;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<FlightService> _logger;

    public FlightService(
        ApplicationDbContext context,
        IFlightDataService flightDataService,
        IWeatherService weatherService,
        ILogger<FlightService> logger)
    {
        _context = context;
        _flightDataService = flightDataService;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<IEnumerable<Flight>> GetFlightBoardAsync(string airportCode)
    {
        return await _context.Flights
            .Where(f => f.OriginAirport == airportCode || f.DestinationAirport == airportCode)
            .Where(f => f.ScheduledDeparture.Date == DateTime.Today)
            .OrderBy(f => f.ScheduledDeparture)
            .ToListAsync();
    }

    public async Task<Flight?> GetFlightDetailsAsync(string flightNumber, DateTime date)
    {
        return await _context.Flights
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber && 
                                    f.ScheduledDeparture.Date == date.Date);
    }

    public async Task<WeatherInfo?> GetWeatherAsync(string airportCode)
    {
        try
        {
            var weatherData = await _weatherService.GetWeatherAsync(airportCode);
            if (weatherData == null)
            {
                _logger.LogWarning("No weather data available for airport {AirportCode}", airportCode);
                return null;
            }

            return new WeatherInfo
            {
                Location = weatherData.Location,
                Temperature = weatherData.TemperatureFahrenheit,
                Conditions = weatherData.Conditions,
                Visibility = weatherData.Visibility,
                WindSpeed = weatherData.WindSpeed,
                WindDirection = weatherData.WindDirectionText
            };
        }
        catch (WeatherServiceException ex)
        {
            _logger.LogError(ex, "Failed to retrieve weather data for airport {AirportCode}", airportCode);
            return null;
        }
    }

    public async Task UpdateFlightStatusAsync(string flightNumber, FlightStatus status)
    {
        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            
        if (flight != null)
        {
            flight.Status = status;
            flight.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}