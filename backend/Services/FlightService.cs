using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineSimulationApi.Services;

public class FlightService : IFlightService
{
    private readonly ApplicationDbContext _context;

    public FlightService(ApplicationDbContext context)
    {
        _context = context;
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
        // Placeholder implementation - will be replaced with actual API integration
        await Task.Delay(100);
        return new WeatherInfo
        {
            Location = airportCode,
            Temperature = 72.0,
            Conditions = "Clear",
            Visibility = 10.0,
            WindSpeed = 5.0,
            WindDirection = "NW"
        };
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