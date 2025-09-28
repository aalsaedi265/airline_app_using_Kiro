using System.ComponentModel.DataAnnotations;

namespace AirlineSimulationApi.Models;

public class Flight
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string FlightNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Airline { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(3)]
    public string OriginAirport { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(3)]
    public string DestinationAirport { get; set; } = string.Empty;
    
    [Required]
    public DateTime ScheduledDeparture { get; set; }
    
    public DateTime? EstimatedDeparture { get; set; }
    
    [Required]
    public DateTime ScheduledArrival { get; set; }
    
    public DateTime? EstimatedArrival { get; set; }
    
    [Required]
    public FlightStatus Status { get; set; }
    
    [MaxLength(10)]
    public string? Gate { get; set; }
    
    [MaxLength(5)]
    public string? Terminal { get; set; }
    
    [MaxLength(50)]
    public string? Aircraft { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public enum FlightStatus
{
    Scheduled,
    OnTime,
    Delayed,
    Boarding,
    Departed,
    InFlight,
    Arrived,
    Cancelled
}