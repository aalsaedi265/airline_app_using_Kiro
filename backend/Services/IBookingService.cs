using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public interface IBookingService
{
    Task<BookingResult> CreateBookingAsync(BookingRequest request);
    Task<Booking?> GetBookingAsync(string confirmationNumber);
    Task<CheckInResult> CheckInAsync(string confirmationNumber);
    Task<SeatMap> GetSeatMapAsync(string flightNumber, DateTime date);
}

public class BookingRequest
{
    public string FlightNumber { get; set; } = string.Empty;
    public DateTime FlightDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<PassengerInfo> Passengers { get; set; } = new();
    public List<string> SelectedSeats { get; set; } = new();
    public PaymentInfo? PaymentInfo { get; set; }
}

public class PassengerInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public SeatClass SeatClass { get; set; }
}

public class BookingResult
{
    public bool Success { get; set; }
    public string? ConfirmationNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public Booking? Booking { get; set; }
}

public class CheckInResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public BoardingPass? BoardingPass { get; set; }
}

public class BoardingPass
{
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string Gate { get; set; } = string.Empty;
    public DateTime BoardingTime { get; set; }
    public string QrCode { get; set; } = string.Empty;
}

public class SeatMap
{
    public string FlightNumber { get; set; } = string.Empty;
    public List<SeatRow> Rows { get; set; } = new();
}

public class SeatRow
{
    public int RowNumber { get; set; }
    public List<Seat> Seats { get; set; } = new();
}

public class Seat
{
    public string Number { get; set; } = string.Empty;
    public SeatClass Class { get; set; }
    public bool IsAvailable { get; set; }
}

public class PaymentInfo
{
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Cvv { get; set; } = string.Empty;
}