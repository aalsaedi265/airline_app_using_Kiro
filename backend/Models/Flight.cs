namespace AirlineSimulationApi.Models;

public class Flight
{
    public int Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string OriginAirport { get; set; } = string.Empty;
    public string DestinationAirport { get; set; } = string.Empty;
    public DateTime ScheduledDeparture { get; set; }
    public DateTime? EstimatedDeparture { get; set; }
    public DateTime ScheduledArrival { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public FlightStatus Status { get; set; }
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public string? Aircraft { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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