namespace AirlineSimulationApi.Models;

public class Booking
{
    public int Id { get; set; }
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int FlightId { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Flight Flight { get; set; } = null!;
    public List<Passenger> Passengers { get; set; } = new();
    public List<BaggageItem> BaggageItems { get; set; } = new();
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    Completed,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}