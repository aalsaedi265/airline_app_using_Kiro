using Hangfire;
using AirlineSimulationApi.Services;

namespace AirlineSimulationApi.Services;

public class FlightUpdateBackgroundService
{
    private readonly IFlightService _flightService;
    private readonly ILogger<FlightUpdateBackgroundService> _logger;

    public FlightUpdateBackgroundService(
        IFlightService flightService,
        ILogger<FlightUpdateBackgroundService> logger)
    {
        _flightService = flightService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task UpdateFlightDataAsync(string airportCode)
    {
        try
        {
            _logger.LogInformation("Starting scheduled flight data update for airport {AirportCode}", airportCode);
            await _flightService.SyncFlightDataFromExternalApiAsync(airportCode);
            _logger.LogInformation("Completed scheduled flight data update for airport {AirportCode}", airportCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update flight data for airport {AirportCode}", airportCode);
            throw; // Re-throw to trigger Hangfire retry
        }
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task UpdateAllAirportsAsync()
    {
        var airports = new[] { "ORD", "LAX", "JFK", "ATL", "DFW", "DEN", "SFO", "SEA", "LAS", "PHX" };
        
        try
        {
            _logger.LogInformation("Starting scheduled flight data update for all major airports");
            
            var tasks = airports.Select(airport => _flightService.SyncFlightDataFromExternalApiAsync(airport));
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Completed scheduled flight data update for all major airports");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update flight data for all airports");
            throw; // Re-throw to trigger Hangfire retry
        }
    }
}

public static class FlightUpdateJobScheduler
{
    public static void ScheduleRecurringJobs()
    {
        // Update Chicago O'Hare (ORD) every 2 minutes - our primary airport
        RecurringJob.AddOrUpdate<FlightUpdateBackgroundService>(
            "update-ord-flights",
            service => service.UpdateFlightDataAsync("ORD"),
            "*/2 * * * *"); // Every 2 minutes

        // Update all major airports every 10 minutes
        RecurringJob.AddOrUpdate<FlightUpdateBackgroundService>(
            "update-all-airports",
            service => service.UpdateAllAirportsAsync(),
            "*/10 * * * *"); // Every 10 minutes
    }
}