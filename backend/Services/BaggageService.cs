using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineSimulationApi.Services;

public interface IBaggageService
{
    Task<BaggageTrackingResult> TrackBaggageAsync(string trackingNumber);
    Task<BaggageTrackingResult> CreateBaggageAsync(int bookingId, int passengerId, string description);
    Task<BaggageTrackingResult> UpdateBaggageStatusAsync(string trackingNumber, BaggageStatus newStatus);
}

public class BaggageService : IBaggageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BaggageService> _logger;

    public BaggageService(ApplicationDbContext context, ILogger<BaggageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BaggageTrackingResult> TrackBaggageAsync(string trackingNumber)
    {
        try
        {
            _logger.LogInformation("Tracking baggage with number {TrackingNumber}", trackingNumber);

            var baggage = await _context.BaggageItems
                .Include(b => b.Booking)
                .ThenInclude(booking => booking.Flight)
                .FirstOrDefaultAsync(b => b.TrackingNumber == trackingNumber);

            if (baggage == null)
            {
                return new BaggageTrackingResult
                {
                    Success = false,
                    ErrorMessage = "Baggage not found with the provided tracking number"
                };
            }

            var result = new BaggageTrackingResult
            {
                Success = true,
                Baggage = new BaggageInfo
                {
                    TrackingNumber = baggage.TrackingNumber,
                    Description = baggage.Type.ToString(),
                    Status = baggage.Status.ToString(),
                    CurrentLocation = GetLocationByStatus(baggage.Status),
                    LastUpdated = baggage.CreatedAt,
                    FlightNumber = baggage.Booking.Flight.FlightNumber,
                    PassengerName = "Passenger", // Simplified for demo
                    Weight = baggage.Weight,
                    StatusHistory = GenerateStatusHistory(baggage.Status)
                }
            };

            _logger.LogInformation("Baggage tracking successful for {TrackingNumber}", trackingNumber);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking baggage {TrackingNumber}", trackingNumber);
            return new BaggageTrackingResult
            {
                Success = false,
                ErrorMessage = "An error occurred while tracking baggage"
            };
        }
    }

    public async Task<BaggageTrackingResult> CreateBaggageAsync(int bookingId, int passengerId, string description)
    {
        try
        {
            _logger.LogInformation("Creating baggage for booking {BookingId}, passenger {PassengerId}", bookingId, passengerId);

            var trackingNumber = GenerateTrackingNumber();
            var baggage = new BaggageItem
            {
                TrackingNumber = trackingNumber,
                BookingId = bookingId,
                Type = BaggageType.Checked,
                Status = BaggageStatus.CheckedIn,
                Weight = new Random().Next(15, 50), // Random weight between 15-50 lbs
                CreatedAt = DateTime.UtcNow
            };

            _context.BaggageItems.Add(baggage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Baggage created successfully with tracking number {TrackingNumber}", trackingNumber);

            return new BaggageTrackingResult
            {
                Success = true,
                Baggage = new BaggageInfo
                {
                    TrackingNumber = trackingNumber,
                    Description = description,
                    Status = "Checked",
                    CurrentLocation = "Check-in Counter",
                    LastUpdated = DateTime.UtcNow,
                    Weight = baggage.Weight,
                    StatusHistory = new List<BaggageStatusUpdate>
                    {
                        new BaggageStatusUpdate
                        {
                            Status = "Checked",
                            Location = "Check-in Counter",
                            Timestamp = DateTime.UtcNow,
                            Description = "Baggage checked in at airport"
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating baggage for booking {BookingId}", bookingId);
            return new BaggageTrackingResult
            {
                Success = false,
                ErrorMessage = "An error occurred while creating baggage"
            };
        }
    }

    public async Task<BaggageTrackingResult> UpdateBaggageStatusAsync(string trackingNumber, BaggageStatus newStatus)
    {
        try
        {
            _logger.LogInformation("Updating baggage status for {TrackingNumber} to {NewStatus}", trackingNumber, newStatus);

            var baggage = await _context.BaggageItems
                .FirstOrDefaultAsync(b => b.TrackingNumber == trackingNumber);

            if (baggage == null)
            {
                return new BaggageTrackingResult
                {
                    Success = false,
                    ErrorMessage = "Baggage not found"
                };
            }

            baggage.Status = newStatus;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Baggage status updated successfully for {TrackingNumber}", trackingNumber);

            return new BaggageTrackingResult
            {
                Success = true,
                Baggage = new BaggageInfo
                {
                    TrackingNumber = trackingNumber,
                    Description = baggage.Type.ToString(),
                    Status = newStatus.ToString(),
                    CurrentLocation = GetLocationByStatus(newStatus),
                    LastUpdated = DateTime.UtcNow,
                    Weight = baggage.Weight,
                    StatusHistory = GenerateStatusHistory(newStatus)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating baggage status for {TrackingNumber}", trackingNumber);
            return new BaggageTrackingResult
            {
                Success = false,
                ErrorMessage = "An error occurred while updating baggage status"
            };
        }
    }

    private string GenerateTrackingNumber()
    {
        var random = new Random();
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var numbers = "0123456789";
        
        var letterPart = new string(Enumerable.Repeat(letters, 3)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        var numberPart = new string(Enumerable.Repeat(numbers, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        return $"{letterPart}{numberPart}";
    }

    private string GetLocationByStatus(BaggageStatus status)
    {
        return status switch
        {
            BaggageStatus.CheckedIn => "Check-in Counter",
            BaggageStatus.InTransit => "In Transit to Aircraft",
            BaggageStatus.Loaded => "Loaded on Aircraft",
            BaggageStatus.Delivered => "Delivered to Baggage Claim",
            BaggageStatus.Lost => "Lost - Under Investigation",
            _ => "Unknown Location"
        };
    }

    private List<BaggageStatusUpdate> GenerateStatusHistory(BaggageStatus currentStatus)
    {
        var history = new List<BaggageStatusUpdate>();
        var random = new Random();
        var baseTime = DateTime.UtcNow.AddHours(-random.Next(1, 12));

        var statuses = new[]
        {
            (BaggageStatus.CheckedIn, "Checked in at airport"),
            (BaggageStatus.InTransit, "Transferred to aircraft loading area"),
            (BaggageStatus.Loaded, "Loaded onto aircraft"),
            (BaggageStatus.Delivered, "Delivered to baggage claim area")
        };

        foreach (var (status, description) in statuses)
        {
            if (status <= currentStatus)
            {
                history.Add(new BaggageStatusUpdate
                {
                    Status = status.ToString(),
                    Location = GetLocationByStatus(status),
                    Timestamp = baseTime.AddMinutes(random.Next(30, 180)),
                    Description = description
                });
            }
        }

        return history.OrderBy(h => h.Timestamp).ToList();
    }
}

// DTOs
public class BaggageTrackingResult
{
    public bool Success { get; set; }
    public BaggageInfo? Baggage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BaggageInfo
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string? FlightNumber { get; set; }
    public string? PassengerName { get; set; }
    public decimal Weight { get; set; }
    public List<BaggageStatusUpdate> StatusHistory { get; set; } = new();
}

public class BaggageStatusUpdate
{
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
}
