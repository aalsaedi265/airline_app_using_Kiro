using AirlineSimulationApi.Models;
using AirlineSimulationApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using AirlineSimulationApi.Hubs;

namespace AirlineSimulationApi.Services;

public class FlightService : IFlightService
{
    private readonly ApplicationDbContext _context;
    private readonly IFlightDataService _flightDataService;
    private readonly IWeatherService _weatherService;
    private readonly IDistributedCache _cache;
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
        IDistributedCache cache,
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
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Flight board data retrieved from cache for airport {AirportCode}", airportCode);
                var cachedFlights = JsonSerializer.Deserialize<List<Flight>>(cachedData);
                return cachedFlights ?? new List<Flight>();
            }

            // Get from database
            var flights = await _context.Flights
                .Where(f => f.OriginAirport == airportCode || f.DestinationAirport == airportCode)
                .Where(f => f.ScheduledDeparture.Date == DateTime.Today)
                .OrderBy(f => f.ScheduledDeparture)
                .ToListAsync();

            // Cache the results
            var serializedFlights = JsonSerializer.Serialize(flights);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            
            await _cache.SetStringAsync(cacheKey, serializedFlights, cacheOptions);
            _logger.LogDebug("Flight board data cached for airport {AirportCode}", airportCode);

            return flights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight board for airport {AirportCode}", airportCode);
            
            // Fallback to database without caching
            return await _context.Flights
                .Where(f => f.OriginAirport == airportCode || f.DestinationAirport == airportCode)
                .Where(f => f.ScheduledDeparture.Date == DateTime.Today)
                .OrderBy(f => f.ScheduledDeparture)
                .ToListAsync();
        }
    }

    public async Task<Flight?> GetFlightDetailsAsync(string flightNumber, DateTime date)
    {
        var cacheKey = $"{FlightDetailsCacheKeyPrefix}:{flightNumber}:{date:yyyy-MM-dd}";
        
        try
        {
            // Try to get from cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Flight details retrieved from cache for flight {FlightNumber}", flightNumber);
                return JsonSerializer.Deserialize<Flight>(cachedData);
            }

            // Get from database
            var flight = await _context.Flights
                .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber && 
                                        f.ScheduledDeparture.Date == date.Date);

            if (flight != null)
            {
                // Cache the results
                var serializedFlight = JsonSerializer.Serialize(flight);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
                };
                
                await _cache.SetStringAsync(cacheKey, serializedFlight, cacheOptions);
                _logger.LogDebug("Flight details cached for flight {FlightNumber}", flightNumber);
            }

            return flight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flight details for flight {FlightNumber}", flightNumber);
            
            // Fallback to database without caching
            return await _context.Flights
                .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber && 
                                        f.ScheduledDeparture.Date == date.Date);
        }
    }

    public async Task<WeatherInfo?> GetWeatherAsync(string airportCode)
    {
        var cacheKey = $"{WeatherCacheKeyPrefix}:{airportCode}";
        
        try
        {
            // Try to get from cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Weather data retrieved from cache for airport {AirportCode}", airportCode);
                return JsonSerializer.Deserialize<WeatherInfo>(cachedData);
            }

            var weatherData = await _weatherService.GetWeatherAsync(airportCode);
            if (weatherData == null)
            {
                _logger.LogWarning("No weather data available for airport {AirportCode}", airportCode);
                return null;
            }

            var weatherInfo = new WeatherInfo
            {
                Location = weatherData.Location,
                Temperature = weatherData.TemperatureFahrenheit,
                Conditions = weatherData.Conditions,
                Visibility = weatherData.Visibility,
                WindSpeed = weatherData.WindSpeed,
                WindDirection = weatherData.WindDirectionText
            };

            // Cache weather data for 10 minutes (weather changes less frequently)
            var serializedWeather = JsonSerializer.Serialize(weatherInfo);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            
            await _cache.SetStringAsync(cacheKey, serializedWeather, cacheOptions);
            _logger.LogDebug("Weather data cached for airport {AirportCode}", airportCode);

            return weatherInfo;
        }
        catch (WeatherServiceException ex)
        {
            _logger.LogError(ex, "Failed to retrieve weather data for airport {AirportCode}", airportCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving weather data for airport {AirportCode}", airportCode);
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
            await _hubContext.Clients.Group($"flight_{flightNumber}")
                .SendAsync("FlightStatusChanged", new
                {
                    FlightNumber = flightNumber,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = status.ToString(),
                    UpdatedAt = DateTime.UtcNow
                });

            _logger.LogInformation("Flight {FlightNumber} status updated from {OldStatus} to {NewStatus}", 
                flightNumber, oldStatus, status);
        }
    }

    public async Task SyncFlightDataFromExternalApiAsync(string airportCode)
    {
        try
        {
            _logger.LogInformation("Starting flight data sync for airport {AirportCode}", airportCode);
            
            var externalFlights = await _flightDataService.GetFlightDataAsync(airportCode);
            var updatedCount = 0;
            var addedCount = 0;

            foreach (var externalFlight in externalFlights)
            {
                var existingFlight = await _context.Flights
                    .FirstOrDefaultAsync(f => f.FlightNumber == externalFlight.FlightNumber &&
                                            f.ScheduledDeparture.Date == externalFlight.ScheduledDeparture.Date);

                if (existingFlight != null)
                {
                    // Update existing flight
                    var statusChanged = UpdateFlightFromExternalData(existingFlight, externalFlight);
                    if (statusChanged)
                    {
                        updatedCount++;
                        
                        // Send real-time update
                        await _hubContext.Clients.Group($"flight_{existingFlight.FlightNumber}")
                            .SendAsync("FlightStatusChanged", new
                            {
                                FlightNumber = existingFlight.FlightNumber,
                                Status = existingFlight.Status.ToString(),
                                EstimatedDeparture = existingFlight.EstimatedDeparture,
                                EstimatedArrival = existingFlight.EstimatedArrival,
                                Gate = existingFlight.Gate,
                                UpdatedAt = DateTime.UtcNow
                            });
                    }
                }
                else
                {
                    // Add new flight
                    var newFlight = CreateFlightFromExternalData(externalFlight);
                    _context.Flights.Add(newFlight);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();
            
            // Invalidate flight board cache
            await InvalidateFlightBoardCacheAsync(airportCode);

            _logger.LogInformation("Flight data sync completed for airport {AirportCode}. Added: {AddedCount}, Updated: {UpdatedCount}", 
                airportCode, addedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing flight data for airport {AirportCode}", airportCode);
            throw;
        }
    }

    private bool UpdateFlightFromExternalData(Flight existingFlight, ExternalFlightData externalFlight)
    {
        var hasChanges = false;

        if (existingFlight.EstimatedDeparture != externalFlight.EstimatedDeparture)
        {
            existingFlight.EstimatedDeparture = externalFlight.EstimatedDeparture;
            hasChanges = true;
        }

        if (existingFlight.EstimatedArrival != externalFlight.EstimatedArrival)
        {
            existingFlight.EstimatedArrival = externalFlight.EstimatedArrival;
            hasChanges = true;
        }

        if (Enum.TryParse<FlightStatus>(externalFlight.Status, true, out var newStatus) && 
            existingFlight.Status != newStatus)
        {
            existingFlight.Status = newStatus;
            hasChanges = true;
        }

        if (existingFlight.Gate != externalFlight.Gate)
        {
            existingFlight.Gate = externalFlight.Gate;
            hasChanges = true;
        }

        if (existingFlight.Terminal != externalFlight.Terminal)
        {
            existingFlight.Terminal = externalFlight.Terminal;
            hasChanges = true;
        }

        if (hasChanges)
        {
            existingFlight.UpdatedAt = DateTime.UtcNow;
        }

        return hasChanges;
    }

    private Flight CreateFlightFromExternalData(ExternalFlightData externalFlight)
    {
        return new Flight
        {
            FlightNumber = externalFlight.FlightNumber,
            Airline = externalFlight.Airline,
            OriginAirport = externalFlight.OriginAirport,
            DestinationAirport = externalFlight.DestinationAirport,
            ScheduledDeparture = externalFlight.ScheduledDeparture,
            EstimatedDeparture = externalFlight.EstimatedDeparture,
            ScheduledArrival = externalFlight.ScheduledArrival,
            EstimatedArrival = externalFlight.EstimatedArrival,
            Status = Enum.TryParse<FlightStatus>(externalFlight.Status, true, out var status) ? status : FlightStatus.Scheduled,
            Gate = externalFlight.Gate,
            Terminal = externalFlight.Terminal,
            Aircraft = externalFlight.Aircraft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task InvalidateFlightCacheAsync(string flightNumber, DateTime flightDate)
    {
        try
        {
            var cacheKey = $"{FlightDetailsCacheKeyPrefix}:{flightNumber}:{flightDate:yyyy-MM-dd}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Invalidated cache for flight {FlightNumber}", flightNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for flight {FlightNumber}", flightNumber);
        }
    }

    private async Task InvalidateFlightBoardCacheAsync(string airportCode)
    {
        try
        {
            var cacheKey = $"{FlightBoardCacheKeyPrefix}:{airportCode}:{DateTime.Today:yyyy-MM-dd}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Invalidated flight board cache for airport {AirportCode}", airportCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate flight board cache for airport {AirportCode}", airportCode);
        }
    }
}