using Microsoft.AspNetCore.Mvc;
using AirlineSimulationApi.Services;
using AirlineSimulationApi.Models;
using System.ComponentModel.DataAnnotations;

namespace AirlineSimulationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(IFlightService flightService, ILogger<FlightsController> logger)
    {
        _flightService = flightService;
        _logger = logger;
    }

    /// <summary>
    /// Get flight board data for a specific airport with optional search and filtering
    /// </summary>
    /// <param name="airport">Airport code (default: ORD for Chicago O'Hare)</param>
    /// <param name="search">Search term to filter by airline, flight number, or destination</param>
    /// <param name="status">Filter by flight status</param>
    /// <param name="airline">Filter by airline</param>
    /// <returns>Flight board data</returns>
    [HttpGet("board")]
    public async Task<ActionResult<FlightBoardResponse>> GetFlightBoard(
        [FromQuery] string airport = "ORD",
        [FromQuery] string? search = null,
        [FromQuery] FlightStatus? status = null,
        [FromQuery] string? airline = null)
    {
        try
        {
            // Validate airport code
            if (string.IsNullOrWhiteSpace(airport) || airport.Length != 3)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Airport code must be a valid 3-letter IATA code",
                    Details = new[] { "Airport code is required and must be exactly 3 characters" }
                });
            }

            airport = airport.ToUpperInvariant();
            _logger.LogInformation("Retrieving flight board for airport {Airport} with search: {Search}, status: {Status}, airline: {Airline}", 
                airport, search, status, airline);

            var flights = await _flightService.GetFlightBoardAsync(airport);
            
            // Apply search and filtering
            var filteredFlights = ApplyFilters(flights, search, status, airline);

            var response = new FlightBoardResponse
            {
                Airport = airport,
                Flights = filteredFlights.Select(MapToFlightSummary).ToList(),
                LastUpdated = DateTime.UtcNow,
                TotalCount = filteredFlights.Count()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight board for airport {Airport}", airport);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving flight information",
                Details = new[] { "Please try again later or contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get detailed information for a specific flight
    /// </summary>
    /// <param name="flightNumber">Flight number (e.g., AA123)</param>
    /// <param name="date">Flight date (optional, defaults to today)</param>
    /// <returns>Detailed flight information</returns>
    [HttpGet("{flightNumber}")]
    public async Task<ActionResult<FlightDetailsResponse>> GetFlightDetails(
        string flightNumber,
        [FromQuery] DateTime? date = null)
    {
        try
        {
            // Validate flight number
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Flight number is required",
                    Details = new[] { "Please provide a valid flight number" }
                });
            }

            var flightDate = date ?? DateTime.Today;
            flightNumber = flightNumber.ToUpperInvariant();

            _logger.LogInformation("Retrieving flight details for {FlightNumber} on {Date}", flightNumber, flightDate);

            var flight = await _flightService.GetFlightDetailsAsync(flightNumber, flightDate);
            
            if (flight == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Flight {flightNumber} not found for date {flightDate:yyyy-MM-dd}",
                    Details = new[] { "Please verify the flight number and date" }
                });
            }

            var response = new FlightDetailsResponse
            {
                Flight = MapToFlightDetails(flight),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight details for {FlightNumber}", flightNumber);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving flight details",
                Details = new[] { "Please try again later or contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get weather information for a flight's origin and destination
    /// </summary>
    /// <param name="flightNumber">Flight number</param>
    /// <param name="date">Flight date (optional, defaults to today)</param>
    /// <returns>Weather information for origin and destination airports</returns>
    [HttpGet("{flightNumber}/weather")]
    public async Task<ActionResult<WeatherResponse>> GetFlightWeather(
        string flightNumber,
        [FromQuery] DateTime? date = null)
    {
        try
        {
            // Validate flight number
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Flight number is required",
                    Details = new[] { "Please provide a valid flight number" }
                });
            }

            var flightDate = date ?? DateTime.Today;
            flightNumber = flightNumber.ToUpperInvariant();

            _logger.LogInformation("Retrieving weather for flight {FlightNumber} on {Date}", flightNumber, flightDate);

            // First get the flight to determine origin and destination
            var flight = await _flightService.GetFlightDetailsAsync(flightNumber, flightDate);
            
            if (flight == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = $"Flight {flightNumber} not found for date {flightDate:yyyy-MM-dd}",
                    Details = new[] { "Please verify the flight number and date" }
                });
            }

            // Get weather for both airports
            var originWeatherTask = _flightService.GetWeatherAsync(flight.OriginAirport);
            var destinationWeatherTask = _flightService.GetWeatherAsync(flight.DestinationAirport);

            await Task.WhenAll(originWeatherTask, destinationWeatherTask);

            var originWeather = await originWeatherTask;
            var destinationWeather = await destinationWeatherTask;

            var response = new WeatherResponse
            {
                FlightNumber = flightNumber,
                OriginAirport = flight.OriginAirport,
                DestinationAirport = flight.DestinationAirport,
                OriginWeather = originWeather,
                DestinationWeather = destinationWeather,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weather for flight {FlightNumber}", flightNumber);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving weather information",
                Details = new[] { "Please try again later or contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get seat map for a specific flight
    /// </summary>
    /// <param name="flightNumber">Flight number</param>
    /// <param name="date">Flight date (optional, defaults to today)</param>
    /// <returns>Seat map with available seats</returns>
    [HttpGet("{flightNumber}/seats")]
    public async Task<ActionResult<SeatMapResponse>> GetSeatMap(
        string flightNumber,
        [FromQuery] DateTime? date = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Flight number is required",
                    Details = new[] { "Please provide a valid flight number" }
                });
            }

            var flightDate = date ?? DateTime.Today;
            flightNumber = flightNumber.ToUpperInvariant();

            _logger.LogInformation("Retrieving seat map for flight {FlightNumber} on {Date}", flightNumber, flightDate);

            // For now, return a mock seat map
            // In a real application, this would come from the database
            var seatMap = GenerateMockSeatMap(flightNumber);

            var response = new SeatMapResponse
            {
                FlightNumber = flightNumber,
                Rows = seatMap.Rows.Select(row => new SeatRowDto
                {
                    RowNumber = row.RowNumber,
                    Seats = row.Seats.Select(seat => new SeatDto
                    {
                        Number = seat.Number,
                        Class = seat.Class.ToString(),
                        IsAvailable = seat.IsAvailable
                    }).ToList()
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seat map for flight {FlightNumber}", flightNumber);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving seat map",
                Details = new[] { "Please try again later or contact support if the problem persists" }
            });
        }
    }

    private IEnumerable<Flight> ApplyFilters(IEnumerable<Flight> flights, string? search, FlightStatus? status, string? airline)
    {
        var filteredFlights = flights;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.ToLowerInvariant();
            filteredFlights = filteredFlights.Where(f =>
                f.FlightNumber.ToLowerInvariant().Contains(searchTerm) ||
                f.Airline.ToLowerInvariant().Contains(searchTerm) ||
                f.DestinationAirport.ToLowerInvariant().Contains(searchTerm) ||
                f.OriginAirport.ToLowerInvariant().Contains(searchTerm));
        }

        // Apply status filter
        if (status.HasValue)
        {
            filteredFlights = filteredFlights.Where(f => f.Status == status.Value);
        }

        // Apply airline filter
        if (!string.IsNullOrWhiteSpace(airline))
        {
            var airlineFilter = airline.ToLowerInvariant();
            filteredFlights = filteredFlights.Where(f => f.Airline.ToLowerInvariant().Contains(airlineFilter));
        }

        return filteredFlights;
    }

    private SeatMap GenerateMockSeatMap(string flightNumber)
    {
        var rows = new List<SeatRow>();
        var random = new Random();
        
        for (int i = 1; i <= 30; i++)
        {
            var seats = new List<Seat>();
            var seatLetters = new[] { "A", "B", "C", "D", "E", "F" };
            
            foreach (var letter in seatLetters)
            {
                var seatNumber = $"{i}{letter}";
                var isAvailable = random.Next(100) < 70; // 70% chance of being available
                
                seats.Add(new Seat
                {
                    Number = seatNumber,
                    Class = SeatClass.Economy, // Simplified - all economy for now
                    IsAvailable = isAvailable
                });
            }
            
            rows.Add(new SeatRow
            {
                RowNumber = i,
                Seats = seats
            });
        }
        
        return new SeatMap
        {
            FlightNumber = flightNumber,
            Rows = rows
        };
    }

    private FlightSummary MapToFlightSummary(Flight flight)
    {
        return new FlightSummary
        {
            Id = flight.Id,
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            OriginAirport = flight.OriginAirport,
            DestinationAirport = flight.DestinationAirport,
            ScheduledDeparture = flight.ScheduledDeparture,
            EstimatedDeparture = flight.EstimatedDeparture,
            ScheduledArrival = flight.ScheduledArrival,
            EstimatedArrival = flight.EstimatedArrival,
            Status = flight.Status,
            Gate = flight.Gate,
            Terminal = flight.Terminal
        };
    }

    private FlightDetails MapToFlightDetails(Flight flight)
    {
        return new FlightDetails
        {
            Id = flight.Id,
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            OriginAirport = flight.OriginAirport,
            DestinationAirport = flight.DestinationAirport,
            ScheduledDeparture = flight.ScheduledDeparture,
            EstimatedDeparture = flight.EstimatedDeparture,
            ScheduledArrival = flight.ScheduledArrival,
            EstimatedArrival = flight.EstimatedArrival,
            Status = flight.Status,
            Gate = flight.Gate,
            Terminal = flight.Terminal,
            Aircraft = flight.Aircraft,
            CreatedAt = flight.CreatedAt,
            UpdatedAt = flight.UpdatedAt
        };
    }
}

// Response DTOs
public class FlightBoardResponse
{
    public string Airport { get; set; } = string.Empty;
    public List<FlightSummary> Flights { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public int TotalCount { get; set; }
}

public class FlightDetailsResponse
{
    public FlightDetails Flight { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class WeatherResponse
{
    public string FlightNumber { get; set; } = string.Empty;
    public string OriginAirport { get; set; } = string.Empty;
    public string DestinationAirport { get; set; } = string.Empty;
    public WeatherInfo? OriginWeather { get; set; }
    public WeatherInfo? DestinationWeather { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class FlightSummary
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
}

public class FlightDetails : FlightSummary
{
    public string? Aircraft { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string> Details { get; set; } = new List<string>();
}

public class SeatMapResponse
{
    public string FlightNumber { get; set; } = string.Empty;
    public List<SeatRowDto> Rows { get; set; } = new();
}

public class SeatRowDto
{
    public int RowNumber { get; set; }
    public List<SeatDto> Seats { get; set; } = new();
}

public class SeatDto
{
    public string Number { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}