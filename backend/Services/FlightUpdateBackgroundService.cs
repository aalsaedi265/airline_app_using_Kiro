using AirlineSimulationApi.Services;
using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public class FlightUpdateBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FlightUpdateBackgroundService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(2);

    public FlightUpdateBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FlightUpdateBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Flight Update Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateFlightStatuses();
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Flight Update Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Flight Update Background Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task UpdateFlightStatuses()
    {
        using var scope = _serviceProvider.CreateScope();
        var flightService = scope.ServiceProvider.GetRequiredService<IFlightService>();
        var random = new Random();

        try
        {
            // Simulate random flight status updates
            var flights = await flightService.GetFlightBoardAsync("ORD");
            
            foreach (var flight in flights.Take(5)) // Update up to 5 random flights
            {
                if (random.Next(100) < 10) // 10% chance of status change
                {
                    var statusChange = GetRandomStatusChange();
                    
                    switch (statusChange)
                    {
                        case "delay":
                            var delayMinutes = random.Next(15, 120);
                            await flightService.UpdateFlightDelayAsync(flight.FlightNumber, delayMinutes, "Weather delay");
                            _logger.LogInformation("Flight {FlightNumber} delayed by {DelayMinutes} minutes", 
                                flight.FlightNumber, delayMinutes);
                            break;
                            
                        case "gate_change":
                            var newGate = $"B{random.Next(1, 10)}";
                            await flightService.UpdateFlightGateAsync(flight.FlightNumber, newGate, "2");
                            _logger.LogInformation("Flight {FlightNumber} gate changed to {NewGate}", 
                                flight.FlightNumber, newGate);
                            break;
                            
                        case "status_change":
                            var newStatus = GetRandomStatus();
                            await flightService.UpdateFlightStatusAsync(flight.FlightNumber, newStatus);
                            _logger.LogInformation("Flight {FlightNumber} status changed to {NewStatus}", 
                                flight.FlightNumber, newStatus);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight statuses");
        }
    }

    private string GetRandomStatusChange()
    {
        var changes = new[] { "delay", "gate_change", "status_change" };
        var random = new Random();
        return changes[random.Next(changes.Length)];
    }

    private FlightStatus GetRandomStatus()
    {
        var statuses = new[] 
        { 
            FlightStatus.OnTime, 
            FlightStatus.Delayed, 
            FlightStatus.Boarding, 
            FlightStatus.Cancelled 
        };
        var random = new Random();
        return statuses[random.Next(statuses.Length)];
    }
}