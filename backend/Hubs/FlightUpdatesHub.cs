using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace AirlineSimulationApi.Hubs;

public class FlightUpdatesHub : Hub
{
    private readonly ILogger<FlightUpdatesHub> _logger;

    public FlightUpdatesHub(ILogger<FlightUpdatesHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a flight group to receive real-time updates for a specific flight
    /// </summary>
    /// <param name="flightNumber">The flight number to subscribe to</param>
    public async Task JoinFlightGroup(string flightNumber)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            await Clients.Caller.SendAsync("Error", "Flight number cannot be empty");
            return;
        }

        var groupName = $"flight_{flightNumber.ToUpperInvariant()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedFlightGroup", flightNumber);
        
        _logger.LogDebug("Connection {ConnectionId} joined flight group {FlightNumber}", 
            Context.ConnectionId, flightNumber);
    }

    /// <summary>
    /// Leave a flight group to stop receiving updates for a specific flight
    /// </summary>
    /// <param name="flightNumber">The flight number to unsubscribe from</param>
    public async Task LeaveFlightGroup(string flightNumber)
    {
        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            await Clients.Caller.SendAsync("Error", "Flight number cannot be empty");
            return;
        }

        var groupName = $"flight_{flightNumber.ToUpperInvariant()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftFlightGroup", flightNumber);
        
        _logger.LogDebug("Connection {ConnectionId} left flight group {FlightNumber}", 
            Context.ConnectionId, flightNumber);
    }

    /// <summary>
    /// Join the general flight board group to receive updates for all flights at an airport
    /// </summary>
    /// <param name="airportCode">The airport code (e.g., ORD, LAX)</param>
    public async Task JoinAirportGroup(string airportCode)
    {
        if (string.IsNullOrWhiteSpace(airportCode))
        {
            await Clients.Caller.SendAsync("Error", "Airport code cannot be empty");
            return;
        }

        var groupName = $"airport_{airportCode.ToUpperInvariant()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedAirportGroup", airportCode);
        
        _logger.LogDebug("Connection {ConnectionId} joined airport group {AirportCode}", 
            Context.ConnectionId, airportCode);
    }

    /// <summary>
    /// Leave the airport group to stop receiving general flight board updates
    /// </summary>
    /// <param name="airportCode">The airport code to unsubscribe from</param>
    public async Task LeaveAirportGroup(string airportCode)
    {
        if (string.IsNullOrWhiteSpace(airportCode))
        {
            await Clients.Caller.SendAsync("Error", "Airport code cannot be empty");
            return;
        }

        var groupName = $"airport_{airportCode.ToUpperInvariant()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftAirportGroup", airportCode);
        
        _logger.LogDebug("Connection {ConnectionId} left airport group {AirportCode}", 
            Context.ConnectionId, airportCode);
    }

    /// <summary>
    /// Subscribe to notifications for a specific user (requires authentication)
    /// </summary>
    [Authorize]
    public async Task JoinUserGroup()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        var groupName = $"user_{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedUserGroup", userId);
        
        _logger.LogDebug("Connection {ConnectionId} joined user group {UserId}", 
            Context.ConnectionId, userId);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            ConnectedAt = DateTime.UtcNow
        });
        
        _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Strongly typed interface for SignalR client methods
/// </summary>
public interface IFlightUpdatesClient
{
    Task FlightStatusChanged(FlightStatusUpdate update);
    Task GateChanged(GateChangeUpdate update);
    Task FlightDelayed(DelayUpdate update);
    Task FlightCancelled(FlightCancellationUpdate update);
    Task FlightBoardUpdated(FlightBoardUpdate update);
    Task Connected(object connectionInfo);
    Task JoinedFlightGroup(string flightNumber);
    Task LeftFlightGroup(string flightNumber);
    Task JoinedAirportGroup(string airportCode);
    Task LeftAirportGroup(string airportCode);
    Task JoinedUserGroup(string userId);
    Task Error(string message);
}

/// <summary>
/// Update models for SignalR messages
/// </summary>
public class FlightStatusUpdate
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime? EstimatedDeparture { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GateChangeUpdate
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string? OldGate { get; set; }
    public string? NewGate { get; set; }
    public string? Terminal { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DelayUpdate
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public DateTime OriginalDeparture { get; set; }
    public DateTime? NewEstimatedDeparture { get; set; }
    public DateTime OriginalArrival { get; set; }
    public DateTime? NewEstimatedArrival { get; set; }
    public int DelayMinutes { get; set; }
    public string? Reason { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FlightCancellationUpdate
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FlightBoardUpdate
{
    public string AirportCode { get; set; } = string.Empty;
    public int TotalFlights { get; set; }
    public int OnTimeFlights { get; set; }
    public int DelayedFlights { get; set; }
    public int CancelledFlights { get; set; }
    public DateTime UpdatedAt { get; set; }
}