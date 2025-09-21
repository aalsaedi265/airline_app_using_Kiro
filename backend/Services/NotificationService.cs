using Microsoft.AspNetCore.SignalR;
using AirlineSimulationApi.Hubs;

namespace AirlineSimulationApi.Services;

public interface INotificationService
{
    Task SendFlightUpdateAsync(string userId, FlightUpdateNotification notification);
    Task SendEmailAsync(string email, string subject, string body);
    Task SendSmsAsync(string phoneNumber, string message);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<FlightUpdatesHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<FlightUpdatesHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendFlightUpdateAsync(string userId, FlightUpdateNotification notification)
    {
        try
        {
            _logger.LogInformation("Sending flight update notification to user {UserId}", userId);

            // Send via SignalR to user's personal group
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("FlightUpdate", notification);

            _logger.LogInformation("Flight update notification sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending flight update notification to user {UserId}", userId);
        }
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        try
        {
            _logger.LogInformation("Sending email to {Email} with subject: {Subject}", email, subject);

            // Simulate email sending delay
            await Task.Delay(500);

            // In a real application, you would integrate with SendGrid, SMTP, etc.
            // For now, we'll just log the email
            _logger.LogInformation("EMAIL SENT TO: {Email}\nSUBJECT: {Subject}\nBODY: {Body}", 
                email, subject, body);

            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", email);
        }
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

            // Simulate SMS sending delay
            await Task.Delay(300);

            // In a real application, you would integrate with Twilio, etc.
            // For now, we'll just log the SMS
            _logger.LogInformation("SMS SENT TO: {PhoneNumber}\nMESSAGE: {Message}", 
                phoneNumber, message);

            _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
        }
    }
}

// DTOs
public class FlightUpdateNotification
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string UpdateType { get; set; } = string.Empty; // "delay", "gate_change", "cancelled", etc.
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}