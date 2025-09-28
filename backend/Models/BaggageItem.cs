using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirlineSimulationApi.Models;

public class BaggageItem
{
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    [MaxLength(12)]
    public string TrackingNumber { get; set; } = string.Empty;
    
    [Required]
    public BaggageType Type { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 999.99)]
    public decimal Weight { get; set; }
    
    [Required]
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