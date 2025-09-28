using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirlineSimulationApi.Models;

public class Booking
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(6)]
    public string ConfirmationNumber { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public int FlightId { get; set; }
    
    [Required]
    public BookingStatus Status { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 999999.99)]
    public decimal TotalAmount { get; set; }
    
    [Required]
    public PaymentStatus PaymentStatus { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Flight Flight { get; set; } = null!;
    public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
    public ICollection<BaggageItem> BaggageItems { get; set; } = new List<BaggageItem>();
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