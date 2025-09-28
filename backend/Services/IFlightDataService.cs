using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public interface IFlightDataService
{
    Task<IEnumerable<ExternalFlightData>> GetFlightDataAsync(string airportCode);
    Task<ExternalFlightData?> GetFlightDetailsAsync(string flightNumber, DateTime date);
    Task<bool> IsServiceAvailableAsync();
}

public class ExternalFlightData
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string AirlineIata { get; set; } = string.Empty;
    public string OriginAirport { get; set; } = string.Empty;
    public string DestinationAirport { get; set; } = string.Empty;
    public DateTime ScheduledDeparture { get; set; }
    public DateTime? EstimatedDeparture { get; set; }
    public DateTime ScheduledArrival { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Gate { get; set; }
    public string? Terminal { get; set; }
    public string? Aircraft { get; set; }
}

public class FlightDataServiceException : Exception
{
    public FlightDataServiceException(string message) : base(message) { }
    public FlightDataServiceException(string message, Exception innerException) : base(message, innerException) { }
}