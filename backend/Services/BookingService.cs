using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineSimulationApi.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(ApplicationDbContext context, IPaymentService paymentService, ILogger<BookingService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<BookingResult> CreateBookingAsync(BookingRequest request)
    {
        try
        {
            // Find the flight
            var flight = await _context.Flights
                .FirstOrDefaultAsync(f => f.FlightNumber == request.FlightNumber && 
                                        f.ScheduledDeparture.Date == request.FlightDate.Date);
            
            if (flight == null)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Flight not found"
                };
            }

            // Check if check-in is available (24 hours before departure)
            var checkInAvailable = DateTime.UtcNow >= flight.ScheduledDeparture.AddHours(-24);
            if (!checkInAvailable)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Check-in not yet available"
                };
            }

            var confirmationNumber = GenerateConfirmationNumber();
            var totalAmount = CalculateTotalAmount(request.Passengers);
            
            // Process payment
            var paymentRequest = new PaymentRequest
            {
                Amount = totalAmount,
                CardNumber = "4111111111111111", // Mock card number
                CardHolderName = request.Passengers.First().FirstName + " " + request.Passengers.First().LastName,
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Cvv = "123",
                Description = $"Flight booking for {flight.FlightNumber}"
            };

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);
            
            if (!paymentResult.Success)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = paymentResult.ErrorMessage ?? "Payment failed"
                };
            }
            
            var booking = new Booking
            {
                ConfirmationNumber = confirmationNumber,
                UserId = request.UserId,
                FlightId = flight.Id,
                Status = BookingStatus.Confirmed,
                PaymentStatus = PaymentStatus.Completed,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // Add passengers
            foreach (var passengerInfo in request.Passengers)
            {
                var passenger = new Passenger
                {
                    BookingId = booking.Id,
                    FirstName = passengerInfo.FirstName,
                    LastName = passengerInfo.LastName,
                    DateOfBirth = passengerInfo.DateOfBirth,
                    SeatClass = passengerInfo.SeatClass,
                    CheckedIn = false
                };
                _context.Passengers.Add(passenger);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking created successfully: {ConfirmationNumber}", confirmationNumber);

            return new BookingResult
            {
                Success = true,
                ConfirmationNumber = confirmationNumber,
                Booking = booking
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return new BookingResult
            {
                Success = false,
                ErrorMessage = "Failed to create booking"
            };
        }
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
        try
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

            // Check if check-in is available
            var checkInAvailable = DateTime.UtcNow >= booking.Flight.ScheduledDeparture.AddHours(-24);
            if (!checkInAvailable)
            {
                return new CheckInResult
                {
                    Success = false,
                    ErrorMessage = "Check-in not yet available. Opens 24 hours before departure."
                };
            }

            // Update booking status
            booking.Status = BookingStatus.CheckedIn;
            await _context.SaveChangesAsync();

            // Generate boarding pass
            var boardingPass = new BoardingPass
            {
                ConfirmationNumber = booking.ConfirmationNumber,
                PassengerName = $"{booking.Passengers.First().FirstName} {booking.Passengers.First().LastName}",
                FlightNumber = booking.Flight.FlightNumber,
                SeatNumber = booking.Passengers.First().SeatNumber ?? "TBD",
                Gate = booking.Flight.Gate ?? "TBD",
                BoardingTime = booking.Flight.ScheduledDeparture.AddMinutes(-30),
                QrCode = $"QR-{booking.ConfirmationNumber}-{DateTime.UtcNow.Ticks}"
            };

            _logger.LogInformation("Check-in completed for booking: {ConfirmationNumber}", confirmationNumber);

            return new CheckInResult
            {
                Success = true,
                BoardingPass = boardingPass
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in for {ConfirmationNumber}", confirmationNumber);
            return new CheckInResult
            {
                Success = false,
                ErrorMessage = "Check-in failed"
            };
        }
    }

    public async Task<SeatMap> GetSeatMapAsync(string flightNumber, DateTime date)
    {
        // Simplified seat map - just return basic structure
        return new SeatMap
        {
            FlightNumber = flightNumber,
            Rows = GenerateSeatRows()
        };
    }

    private string GenerateConfirmationNumber()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private decimal CalculateTotalAmount(List<PassengerInfo> passengers)
    {
        var basePrice = 299.99m;
        var total = 0m;

        foreach (var passenger in passengers)
        {
            var multiplier = passenger.SeatClass switch
            {
                SeatClass.Economy => 1.0m,
                SeatClass.PremiumEconomy => 1.5m,
                SeatClass.Business => 2.5m,
                SeatClass.First => 4.0m,
                _ => 1.0m
            };
            total += basePrice * multiplier;
        }

        return total;
    }

    private List<SeatRow> GenerateSeatRows()
    {
        var rows = new List<SeatRow>();
        for (int i = 1; i <= 30; i++)
        {
            rows.Add(new SeatRow
            {
                RowNumber = i,
                Seats = new List<Seat>
                {
                    new Seat { Number = $"{i}A", Class = SeatClass.Economy, IsAvailable = true },
                    new Seat { Number = $"{i}B", Class = SeatClass.Economy, IsAvailable = true },
                    new Seat { Number = $"{i}C", Class = SeatClass.Economy, IsAvailable = false },
                    new Seat { Number = $"{i}D", Class = SeatClass.Economy, IsAvailable = true },
                    new Seat { Number = $"{i}E", Class = SeatClass.Economy, IsAvailable = true },
                    new Seat { Number = $"{i}F", Class = SeatClass.Economy, IsAvailable = true }
                }
            });
        }
        return rows;
    }

}