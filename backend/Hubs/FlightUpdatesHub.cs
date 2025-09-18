using Microsoft.AspNetCore.SignalR;

namespace AirlineSimulationApi.Hubs;

public class FlightUpdatesHub : Hub
{
    public async Task JoinFlightGroup(string flightNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Flight_{flightNumber}");
        await Clients.Caller.SendAsync("JoinedFlightGroup", flightNumber);
    }

    public async Task LeaveFlightGroup(string flightNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Flight_{flightNumber}");
        await Clients.Caller.SendAsync("LeftFlightGroup", flightNumber);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // Methods to send updates to specific flight groups
    public async Task SendFlightStatusUpdate(string flightNumber, object update)
    {
        await Clients.Group($"Flight_{flightNumber}").SendAsync("FlightStatusChanged", update);
    }

    public async Task SendGateChange(string flightNumber, object update)
    {
        await Clients.Group($"Flight_{flightNumber}").SendAsync("GateChanged", update);
    }

    public async Task SendFlightDelay(string flightNumber, object update)
    {
        await Clients.Group($"Flight_{flightNumber}").SendAsync("FlightDelayed", update);
    }
}