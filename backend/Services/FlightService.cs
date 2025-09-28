using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using AirlineSimulationApi.Hubs;

namespace AirlineSimulationApi.Services;

public class FlightService : IFlightService
{
    private readonly ApplicationDbContext _context;
    private readonly IFlightDataService _flightDataService;
    private readonly IWeatherService _weatherService;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<FlightUpdatesHub> _hubContext;
    private readonly ILogger<FlightService> _logger;
    
    private const int CacheExpirationMinutes = 5;
    private const string FlightBoardCacheKeyPrefix = "flight_board";
    private const string FlightDetailsCacheKeyPrefix = "flight_details";
    private const string WeatherCacheKeyPrefix = "weather";

    public FlightService(
        ApplicationDbContext context,
        IFlightDataService flightDataService,
        IWeatherService weatherService,
        IMemoryCache cache,
        IHubContext<FlightUpdatesHub> hubContext,
        ILogger<FlightService> logger)
    {
        _context = context;
        _flightDataService = flightDataService;
        _weatherService = weatherService;
        _cache = cache;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<IEnumerable<Flight>> GetFlightBoardAsync(string airportCode)
    {
        var cacheKey = $"{FlightBoardCacheKeyPrefix}:{airportCode}:{DateTime.Today:yyyy-MM-dd}";
        
        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out List<Flight>? cachedFlights) && cachedFlights != null)
            {
                _logger.LogDebug("Flight board data retrieved from cache for airport {AirportCode}", airportCode);
                return cachedFlights;
            }

            // Try to get from external API first
            try
            {
                var externalFlights = await _flightDataService.GetFlightDataAsync(airportCode);
                if (externalFlights != null && externalFlights.Any())
                {
                    // Convert external data to Flight objects and save to database
                    foreach (var externalFlight in externalFlights)
                    {
                        var utcScheduledDeparture = externalFlight.ScheduledDeparture.Kind == DateTimeKind.Utc 
                            ? externalFlight.ScheduledDeparture 
                            : DateTime.SpecifyKind(externalFlight.ScheduledDeparture, DateTimeKind.Utc);
                        
                        var existingFlight = await _context.Flights
                            .FirstOrDefaultAsync(f => f.FlightNumber == externalFlight.FlightNumber && 
                                                     f.ScheduledDeparture.Date == utcScheduledDeparture.Date);
                        
                        if (existingFlight == null)
                        {
                            var flight = new Flight
                            {
                                FlightNumber = externalFlight.FlightNumber,
                                Airline = externalFlight.Airline,
                                OriginAirport = externalFlight.OriginAirport,
                                DestinationAirport = externalFlight.DestinationAirport,
                                ScheduledDeparture = externalFlight.ScheduledDeparture.Kind == DateTimeKind.Utc 
                                    ? externalFlight.ScheduledDeparture 
                                    : DateTime.SpecifyKind(externalFlight.ScheduledDeparture, DateTimeKind.Utc),
                                EstimatedDeparture = externalFlight.EstimatedDeparture?.Kind == DateTimeKind.Utc 
                                    ? externalFlight.EstimatedDeparture 
                                    : externalFlight.EstimatedDeparture.HasValue 
                                        ? DateTime.SpecifyKind(externalFlight.EstimatedDeparture.Value, DateTimeKind.Utc)
                                        : null,
                                ScheduledArrival = externalFlight.ScheduledArrival.Kind == DateTimeKind.Utc 
                                    ? externalFlight.ScheduledArrival 
                                    : DateTime.SpecifyKind(externalFlight.ScheduledArrival, DateTimeKind.Utc),
                                EstimatedArrival = externalFlight.EstimatedArrival?.Kind == DateTimeKind.Utc 
                                    ? externalFlight.EstimatedArrival 
                                    : externalFlight.EstimatedArrival.HasValue 
                                        ? DateTime.SpecifyKind(externalFlight.EstimatedArrival.Value, DateTimeKind.Utc)
                                        : null,
                                Status = MapExternalStatusToFlightStatus(externalFlight.Status),
                                Gate = externalFlight.Gate,
                                Terminal = externalFlight.Terminal,
                                Aircraft = externalFlight.Aircraft,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Flights.Add(flight);
                        }
                        else
                        {
                            // Update existing flight with latest data
                            existingFlight.Status = MapExternalStatusToFlightStatus(externalFlight.Status);
                            existingFlight.Gate = externalFlight.Gate;
                            existingFlight.Terminal = externalFlight.Terminal;
                            existingFlight.EstimatedDeparture = externalFlight.EstimatedDeparture?.Kind == DateTimeKind.Utc
                                ? externalFlight.EstimatedDeparture
                                : externalFlight.EstimatedDeparture.HasValue
                                    ? DateTime.SpecifyKind(externalFlight.EstimatedDeparture.Value, DateTimeKind.Utc)
                                    : null;
                            existingFlight.EstimatedArrival = externalFlight.EstimatedArrival?.Kind == DateTimeKind.Utc
                                ? externalFlight.EstimatedArrival
                                : externalFlight.EstimatedArrival.HasValue
                                    ? DateTime.SpecifyKind(externalFlight.EstimatedArrival.Value, DateTimeKind.Utc)
                                    : null;
                            existingFlight.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch external flight data for {AirportCode}, using database data", airportCode);
            }

            // Get from database
            var today = DateTime.UtcNow.Date;
            var flights = await _context.Flights
                .Where(f => f.OriginAirport == airportCode || f.DestinationAirport == airportCode)
                .Where(f => f.ScheduledDeparture.Date == today)
                .OrderBy(f => f.ScheduledDeparture)
                .ToListAsync();

            // Cache the results
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            
            _cache.Set(cacheKey, flights, cacheOptions);
            _logger.LogDebug("Flight board data cached for airport {AirportCode}", airportCode);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight board for airport {AirportCode}", airportCode);
            return new List<Flight>();
        }
    }

    public async Task<Flight?> GetFlightDetailsAsync(string flightNumber, DateTime date)
    {
        var cacheKey = $"{FlightDetailsCacheKeyPrefix}:{flightNumber}:{date:yyyy-MM-dd}";
        
        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out Flight? cachedFlight) && cachedFlight != null)
            {
                _logger.LogDebug("Flight details retrieved from cache for {FlightNumber}", flightNumber);
                return cachedFlight;
            }

            // Get from database
            var flight = await _context.Flights
                .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber && f.ScheduledDeparture.Date == date.Date);

            if (flight != null)
            {
                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
                };
                
                _cache.Set(cacheKey, flight, cacheOptions);
                _logger.LogDebug("Flight details cached for {FlightNumber}", flightNumber);
            }

            return flight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight details for {FlightNumber}", flightNumber);
            return null;
        }
    }

    public async Task<WeatherInfo?> GetWeatherAsync(string airportCode)
    {
        var cacheKey = $"{WeatherCacheKeyPrefix}:{airportCode}";
        
        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out WeatherInfo? cachedWeather) && cachedWeather != null)
            {
                _logger.LogDebug("Weather data retrieved from cache for airport {AirportCode}", airportCode);
                return cachedWeather;
            }

            // Get weather from API
            var weatherData = await GetWeatherFromApiAsync(airportCode);
            var weatherInfo = weatherData != null ? new WeatherInfo
            {
                Location = airportCode,
                Temperature = weatherData.Temperature,
                Conditions = weatherData.Conditions,
                Visibility = weatherData.Visibility,
                WindSpeed = weatherData.WindSpeed,
                WindDirection = weatherData.WindDirectionText
            } : null;

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Weather changes less frequently
            };
            
            _cache.Set(cacheKey, weatherInfo, cacheOptions);
            _logger.LogDebug("Weather data cached for airport {AirportCode}", airportCode);

            return weatherInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weather data for airport {AirportCode}", airportCode);
            return null;
        }
    }

    public async Task UpdateFlightStatusAsync(string flightNumber, FlightStatus status)
    {
        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            
        if (flight != null)
        {
            var oldStatus = flight.Status;
            flight.Status = status;
            flight.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Invalidate cache for this flight
            await InvalidateFlightCacheAsync(flightNumber, flight.ScheduledDeparture);
            
            // Send real-time update via SignalR
            await SendFlightStatusUpdateAsync(flight, oldStatus, status);

            _logger.LogInformation("Flight {FlightNumber} status updated from {OldStatus} to {NewStatus}", 
                flightNumber, oldStatus, status);
        }
    }

    public async Task UpdateFlightGateAsync(string flightNumber, string? newGate, string? terminal = null)
    {
        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            
        if (flight != null)
        {
            var oldGate = flight.Gate;
            flight.Gate = newGate;
            if (!string.IsNullOrEmpty(terminal))
            {
                flight.Terminal = terminal;
            }
            flight.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Invalidate cache for this flight
            await InvalidateFlightCacheAsync(flightNumber, flight.ScheduledDeparture);
            
            // Send real-time update via SignalR
            await SendGateChangeUpdateAsync(flight, oldGate, newGate);

            _logger.LogInformation("Flight {FlightNumber} gate updated from {OldGate} to {NewGate}", 
                flightNumber, oldGate, newGate);
        }
    }

    public async Task UpdateFlightDelayAsync(string flightNumber, int delayMinutes, string? reason = null)
    {
        var flight = await _context.Flights
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
            
        if (flight != null)
        {
            flight.EstimatedDeparture = flight.ScheduledDeparture.AddMinutes(delayMinutes);
            flight.EstimatedArrival = flight.ScheduledArrival.AddMinutes(delayMinutes);
            flight.Status = FlightStatus.Delayed;
            flight.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Invalidate cache for this flight
            await InvalidateFlightCacheAsync(flightNumber, flight.ScheduledDeparture);
            
            // Send real-time update via SignalR
            await SendDelayUpdateAsync(flight, delayMinutes, reason);

            _logger.LogInformation("Flight {FlightNumber} delayed by {DelayMinutes} minutes", 
                flightNumber, delayMinutes);
        }
    }

    private Task InvalidateFlightCacheAsync(string flightNumber, DateTime scheduledDeparture)
    {
        var date = scheduledDeparture.Date;
        var flightBoardKey = $"{FlightBoardCacheKeyPrefix}:*:{date:yyyy-MM-dd}";
        var flightDetailsKey = $"{FlightDetailsCacheKeyPrefix}:{flightNumber}:{date:yyyy-MM-dd}";
        
        // Remove from cache
        _cache.Remove(flightDetailsKey);
        
        // For flight board, we'd need to remove all airport variations
        // This is a simplified approach - in production you might want more sophisticated cache invalidation
        _logger.LogDebug("Cache invalidated for flight {FlightNumber}", flightNumber);
        return Task.CompletedTask;
    }

    private async Task SendFlightStatusUpdateAsync(Flight flight, FlightStatus oldStatus, FlightStatus newStatus)
    {
        var update = new FlightStatusUpdate
        {
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            EstimatedDeparture = flight.EstimatedDeparture,
            EstimatedArrival = flight.EstimatedArrival,
            Gate = flight.Gate,
            Terminal = flight.Terminal,
            UpdatedAt = DateTime.UtcNow
        };

        // Send to flight-specific group
        await _hubContext.Clients.Group($"flight_{flight.FlightNumber.ToUpperInvariant()}")
            .SendAsync("FlightStatusChanged", update);

        // Send to airport groups
        await _hubContext.Clients.Group($"airport_{flight.OriginAirport.ToUpperInvariant()}")
            .SendAsync("FlightStatusChanged", update);
        await _hubContext.Clients.Group($"airport_{flight.DestinationAirport.ToUpperInvariant()}")
            .SendAsync("FlightStatusChanged", update);
    }

    private async Task SendGateChangeUpdateAsync(Flight flight, string? oldGate, string? newGate)
    {
        var update = new GateChangeUpdate
        {
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            OldGate = oldGate,
            NewGate = newGate,
            Terminal = flight.Terminal,
            UpdatedAt = DateTime.UtcNow
        };

        // Send to flight-specific group
        await _hubContext.Clients.Group($"flight_{flight.FlightNumber.ToUpperInvariant()}")
            .SendAsync("GateChanged", update);

        // Send to airport groups
        await _hubContext.Clients.Group($"airport_{flight.OriginAirport.ToUpperInvariant()}")
            .SendAsync("GateChanged", update);
    }

    private async Task SendDelayUpdateAsync(Flight flight, int delayMinutes, string? reason)
    {
        var update = new DelayUpdate
        {
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            OriginalDeparture = flight.ScheduledDeparture,
            NewEstimatedDeparture = flight.EstimatedDeparture,
            OriginalArrival = flight.ScheduledArrival,
            NewEstimatedArrival = flight.EstimatedArrival,
            DelayMinutes = delayMinutes,
            Reason = reason,
            UpdatedAt = DateTime.UtcNow
        };

        // Send to flight-specific group
        await _hubContext.Clients.Group($"flight_{flight.FlightNumber.ToUpperInvariant()}")
            .SendAsync("FlightDelayed", update);

        // Send to airport groups
        await _hubContext.Clients.Group($"airport_{flight.OriginAirport.ToUpperInvariant()}")
            .SendAsync("FlightDelayed", update);
    }

    private async Task<WeatherData?> GetWeatherFromApiAsync(string airportCode)
    {
        try
        {
            var weatherInfo = await _weatherService.GetWeatherAsync(airportCode);
            return weatherInfo != null ? new WeatherData
            {
                Temperature = weatherInfo.Temperature,
                Conditions = weatherInfo.Conditions,
                Visibility = weatherInfo.Visibility,
                WindSpeed = weatherInfo.WindSpeed,
                WindDirection = weatherInfo.WindDirection
            } : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather from API for {AirportCode}", airportCode);
            return null;
        }
    }

    private static FlightStatus MapExternalStatusToFlightStatus(string externalStatus)
    {
        return externalStatus.ToLowerInvariant() switch
        {
            "scheduled" or "active" => FlightStatus.OnTime,
            "delayed" => FlightStatus.Delayed,
            "cancelled" or "canceled" => FlightStatus.Cancelled,
            "boarding" => FlightStatus.Boarding,
            "departed" => FlightStatus.Departed,
            "landed" or "arrived" => FlightStatus.Arrived,
            _ => FlightStatus.OnTime // Default fallback
        };
    }
}

public class WeatherData
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double TemperatureFahrenheit => (Temperature * 9 / 5) + 32;
    public string Conditions { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public double Visibility { get; set; }
    public double WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public string WindDirectionText { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}