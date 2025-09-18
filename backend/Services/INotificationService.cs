using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public interface INotificationService
{
    Task SendFlightUpdateAsync(string userId, Flight flight, string message);
    Task SendBookingConfirmationAsync(string userId, Booking booking);
    Task SendCheckInReminderAsync(string userId, Booking booking);
    Task SendGateChangeNotificationAsync(string userId, Flight flight, string newGate);
}

public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    FlightUpdate,
    BookingConfirmation,
    CheckInReminder,
    GateChange,
    FlightDelay,
    FlightCancellation
}