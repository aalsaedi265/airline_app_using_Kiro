using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AirlineSimulationApi.Services;
using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var bookingRequest = new BookingRequest
            {
                FlightNumber = request.FlightNumber,
                FlightDate = request.FlightDate,
                UserId = userId,
                Passengers = request.Passengers.Select(p => new PassengerInfo
                {
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    DateOfBirth = p.DateOfBirth,
                    SeatClass = p.SeatClass
                }).ToList(),
                SelectedSeats = request.SelectedSeats
            };

            var result = await _bookingService.CreateBookingAsync(bookingRequest);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new BookingResponse
            {
                ConfirmationNumber = result.ConfirmationNumber!,
                Status = result.Booking!.Status.ToString(),
                TotalAmount = result.Booking.TotalAmount,
                CreatedAt = result.Booking.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { message = "Booking creation failed" });
        }
    }

    [HttpGet("{confirmationNumber}")]
    public async Task<ActionResult<BookingDetailsResponse>> GetBooking(string confirmationNumber)
    {
        try
        {
            var booking = await _bookingService.GetBookingAsync(confirmationNumber);
            
            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            return Ok(new BookingDetailsResponse
            {
                ConfirmationNumber = booking.ConfirmationNumber,
                Status = booking.Status.ToString(),
                TotalAmount = booking.TotalAmount,
                Flight = new FlightSummary
                {
                    FlightNumber = booking.Flight.FlightNumber,
                    Airline = booking.Flight.Airline,
                    OriginAirport = booking.Flight.OriginAirport,
                    DestinationAirport = booking.Flight.DestinationAirport,
                    ScheduledDeparture = booking.Flight.ScheduledDeparture,
                    ScheduledArrival = booking.Flight.ScheduledArrival,
                    Status = booking.Flight.Status,
                    Gate = booking.Flight.Gate,
                    Terminal = booking.Flight.Terminal
                },
                Passengers = booking.Passengers.Select(p => new PassengerDto
                {
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    SeatNumber = p.SeatNumber,
                    SeatClass = p.SeatClass.ToString()
                }).ToList(),
                CreatedAt = booking.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking {ConfirmationNumber}", confirmationNumber);
            return StatusCode(500, new { message = "Failed to retrieve booking" });
        }
    }

    [HttpPost("{confirmationNumber}/checkin")]
    public async Task<ActionResult<CheckInResponse>> CheckIn(string confirmationNumber)
    {
        try
        {
            var result = await _bookingService.CheckInAsync(confirmationNumber);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new CheckInResponse
            {
                Success = true,
                BoardingPass = result.BoardingPass
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in for {ConfirmationNumber}", confirmationNumber);
            return StatusCode(500, new { message = "Check-in failed" });
        }
    }
}

// DTOs
public class CreateBookingRequest
{
    public string FlightNumber { get; set; } = string.Empty;
    public DateTime FlightDate { get; set; }
    public List<PassengerRequest> Passengers { get; set; } = new();
    public List<string> SelectedSeats { get; set; } = new();
}

public class PassengerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public SeatClass SeatClass { get; set; }
}

public class BookingResponse
{
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookingDetailsResponse
{
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public FlightSummary Flight { get; set; } = new();
    public List<PassengerDto> Passengers { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CheckInResponse
{
    public bool Success { get; set; }
    public BoardingPass? BoardingPass { get; set; }
}

public class PassengerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? SeatNumber { get; set; }
    public string SeatClass { get; set; } = string.Empty;
}
