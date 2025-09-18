using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineSimulationApi.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookingResult> CreateBookingAsync(BookingRequest request)
    {
        // Placeholder implementation
        var confirmationNumber = GenerateConfirmationNumber();
        
        var booking = new Booking
        {
            ConfirmationNumber = confirmationNumber,
            UserId = request.UserId,
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            TotalAmount = 299.99m // Placeholder amount
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return new BookingResult
        {
            Success = true,
            ConfirmationNumber = confirmationNumber,
            Booking = booking
        };
    }

    public async Task<Booking?> GetBookingAsync(string confirmationNumber)
    {
        return await _context.Bookings
            .Include(b => b.Flight)
            .Include(b => b.Passengers)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.ConfirmationNumber == confirmationNumber);
    }

    public async Task<CheckInResult> CheckInAsync(string confirmationNumber)
    {
        var booking = await GetBookingAsync(confirmationNumber);
        
        if (booking == null)
        {
            return new CheckInResult
            {
                Success = false,
                ErrorMessage = "Booking not found"
            };
        }

        // Placeholder implementation
        return new CheckInResult
        {
            Success = true,
            BoardingPass = new BoardingPass
            {
                ConfirmationNumber = confirmationNumber,
                PassengerName = "John Doe",
                FlightNumber = booking.Flight?.FlightNumber ?? "",
                SeatNumber = "12A",
                Gate = "B15",
                BoardingTime = DateTime.Now.AddHours(2),
                QrCode = "QR_CODE_DATA"
            }
        };
    }

    public async Task<SeatMap> GetSeatMapAsync(string flightNumber, DateTime date)
    {
        // Placeholder implementation
        await Task.Delay(100);
        return new SeatMap
        {
            FlightNumber = flightNumber,
            Rows = new List<SeatRow>()
        };
    }

    private string GenerateConfirmationNumber()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}