namespace AirlineSimulationApi.Models;

public class NotificationPreferences
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    public bool FlightUpdates { get; set; } = true;
    public bool PromotionalOffers { get; set; } = false;
    public bool BookingConfirmations { get; set; } = true;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}