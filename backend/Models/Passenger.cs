using System.ComponentModel.DataAnnotations;

namespace AirlineSimulationApi.Models;

public class Passenger
{
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime? DateOfBirth { get; set; }
    
    [MaxLength(5)]
    public string? SeatNumber { get; set; }
    
    [Required]
    public SeatClass SeatClass { get; set; }
    
    public bool CheckedIn { get; set; } = false;
    public DateTime? CheckInTime { get; set; }
    
    // Navigation properties
    public Booking Booking { get; set; } = null!;
}

public enum SeatClass
{
    Economy,
    PremiumEconomy,
    Business,
    First
}