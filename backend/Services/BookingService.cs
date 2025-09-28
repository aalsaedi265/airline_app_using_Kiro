using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AirlineSimulationApi.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingService> _logger;
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public BookingService(ApplicationDbContext context, IPaymentService paymentService, IEmailService emailService, ILogger<BookingService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BookingResult> CreateBookingAsync(BookingRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.FlightNumber) || string.IsNullOrWhiteSpace(request.UserId))
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Flight number and user ID are required"
                };
            }

            if (request.Passengers == null || !request.Passengers.Any())
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "At least one passenger is required"
                };
            }

            // Validate passenger data
            foreach (var passenger in request.Passengers)
            {
                if (string.IsNullOrWhiteSpace(passenger.FirstName) || string.IsNullOrWhiteSpace(passenger.LastName))
                {
                    return new BookingResult
                    {
                        Success = false,
                        ErrorMessage = "Passenger first name and last name are required"
                    };
                }
            }

            // Find the flight
            var flightDate = request.FlightDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.FlightDate, DateTimeKind.Utc)
                : request.FlightDate.ToUniversalTime();

            var flight = await _context.Flights
                .FirstOrDefaultAsync(f => f.FlightNumber == request.FlightNumber &&
                                        f.ScheduledDeparture.Date == flightDate.Date);
            
            if (flight == null)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Flight not found"
                };
            }

            // Check if booking is still possible (can't book flights in the past)
            if (flight.ScheduledDeparture <= DateTime.UtcNow)
            {
                return new BookingResult
                {
                    Success = false,
                    ErrorMessage = "Cannot book flights that have already departed"
                };
            }

            var confirmationNumber = GenerateConfirmationNumber();
            var totalAmount = CalculateTotalAmount(request.Passengers);

            // For demo purposes, simulate payment processing
            // In production, this would require actual payment info
            var paymentRequest = new PaymentRequest
            {
                Amount = totalAmount,
                CardNumber = "4111111111111111", // Demo card
                CardHolderName = $"{request.UserId} Demo",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.Now.Year + 2,
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
            await _context.SaveChangesAsync(); // Save to get the booking ID

            // Add passengers with the actual booking ID
            foreach (var passengerInfo in request.Passengers)
            {
                DateTime? passengerDateOfBirth = null;
                if (passengerInfo.DateOfBirth.HasValue)
                {
                    var dob = passengerInfo.DateOfBirth.Value;
                    passengerDateOfBirth = dob.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dob, DateTimeKind.Utc)
                        : dob.ToUniversalTime();
                }

                var passenger = new Passenger
                {
                    BookingId = booking.Id,
                    FirstName = passengerInfo.FirstName,
                    LastName = passengerInfo.LastName,
                    DateOfBirth = passengerDateOfBirth,
                    SeatClass = passengerInfo.SeatClass,
                    CheckedIn = false
                };
                _context.Passengers.Add(passenger);
            }

            await _context.SaveChangesAsync(); // Save passengers

            // Get user email for booking confirmation
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null)
            {
                // Load the full booking with navigation properties for email
                var fullBooking = await _context.Bookings
                    .Include(b => b.Flight)
                    .Include(b => b.Passengers)
                    .Include(b => b.User)
                    .FirstAsync(b => b.Id == booking.Id);

                // Send booking confirmation email (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendBookingConfirmationAsync(fullBooking, user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send booking confirmation email for {ConfirmationNumber}", confirmationNumber);
                    }
                });
            }

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

            // Generate boarding pass - safely handle passengers
            var firstPassenger = booking.Passengers?.FirstOrDefault();
            if (firstPassenger == null)
            {
                return new CheckInResult
                {
                    Success = false,
                    ErrorMessage = "No passengers found for this booking"
                };
            }

            var boardingPass = new BoardingPass
            {
                ConfirmationNumber = booking.ConfirmationNumber,
                PassengerName = $"{firstPassenger.FirstName} {firstPassenger.LastName}",
                FlightNumber = booking.Flight.FlightNumber,
                SeatNumber = firstPassenger.SeatNumber ?? "TBD",
                Gate = booking.Flight.Gate ?? "TBD",
                BoardingTime = booking.Flight.ScheduledDeparture.AddMinutes(-30),
                QrCode = GenerateSecureQrCode(booking.ConfirmationNumber)
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

    public Task<SeatMap> GetSeatMapAsync(string flightNumber, DateTime date)
    {
        // Simplified seat map - just return basic structure
        var seatMap = new SeatMap
        {
            FlightNumber = flightNumber,
            Rows = GenerateSeatRows()
        };
        return Task.FromResult(seatMap);
    }

    private string GenerateConfirmationNumber()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[6];
        var buffer = new byte[4];

        for (int i = 0; i < 6; i++)
        {
            _rng.GetBytes(buffer);
            var randomValue = BitConverter.ToUInt32(buffer, 0);
            result[i] = chars[(int)(randomValue % chars.Length)];
        }

        return new string(result);
    }

    private string GenerateSecureQrCode(string confirmationNumber)
    {
        var buffer = new byte[8];
        _rng.GetBytes(buffer);
        var randomSuffix = BitConverter.ToUInt64(buffer, 0);
        return $"QR-{confirmationNumber}-{randomSuffix}";
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