namespace AirlineSimulationApi.Models;

public class Passenger
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? SeatNumber { get; set; }
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