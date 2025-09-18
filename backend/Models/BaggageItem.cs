namespace AirlineSimulationApi.Models;

public class BaggageItem
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public BaggageType Type { get; set; }
    public decimal Weight { get; set; }
    public BaggageStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Booking Booking { get; set; } = null!;
}

public enum BaggageType
{
    CarryOn,
    Checked,
    Oversized,
    Special
}

public enum BaggageStatus
{
    CheckedIn,
    InTransit,
    Loaded,
    Delivered,
    Lost
}