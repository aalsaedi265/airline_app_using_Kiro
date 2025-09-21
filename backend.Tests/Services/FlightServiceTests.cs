using AirlineSimulationApi.Data;
using AirlineSimulationApi.Models;
using AirlineSimulationApi.Services;
using AirlineSimulationApi.Hubs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AirlineSimulationApi.Tests.Services;

public class FlightServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IFlightDataService> _mockFlightDataService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IHubContext<FlightUpdatesHub>> _mockHubContext;
    private readonly Mock<ILogger<FlightService>> _mockLogger;
    private readonly FlightService _flightService;

    public FlightServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockFlightDataService = new Mock<IFlightDataService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _mockCache = new Mock<IDistributedCache>();
        _mockHubContext = new Mock<IHubContext<FlightUpdatesHub>>();
        _mockLogger = new Mock<ILogger<FlightService>>();

        _flightService = new FlightService(
            _context,
            _mockFlightDataService.Object,
            _mockWeatherService.Object,
            _mockCache.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetFlightBoardAsync_WithCachedData_ReturnsCachedFlights()
    {
        // Arrange
        var airportCode = "ORD";
        var cachedFlights = new List<Flight>
        {
            new Flight
            {
                Id = 1,
                FlightNumber = "AA123",
                Airline = "American Airlines",
                OriginAirport = "ORD",
                DestinationAirport = "LAX",
                ScheduledDeparture = DateTime.Today.AddHours(10),
                Status = FlightStatus.OnTime
            }
        };

        var cachedJson = JsonSerializer.Serialize(cachedFlights);
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        // Act
        var result = await _flightService.GetFlightBoardAsync(airportCode);

        // Assert
        result.Should().HaveCount(1);
        result.First().FlightNumber.Should().Be("AA123");
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightBoardAsync_WithoutCachedData_ReturnsFromDatabaseAndCaches()
    {
        // Arrange
        var airportCode = "ORD";
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = DateTime.Today.AddHours(10),
            Status = FlightStatus.OnTime
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _flightService.GetFlightBoardAsync(airportCode);

        // Assert
        result.Should().HaveCount(1);
        result.First().FlightNumber.Should().Be("AA123");
        
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(x => x.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightDetailsAsync_WithCachedData_ReturnsCachedFlight()
    {
        // Arrange
        var flightNumber = "AA123";
        var date = DateTime.Today;
        var cachedFlight = new Flight
        {
            Id = 1,
            FlightNumber = flightNumber,
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = date.AddHours(10),
            Status = FlightStatus.OnTime
        };

        var cachedJson = JsonSerializer.Serialize(cachedFlight);
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        // Act
        var result = await _flightService.GetFlightDetailsAsync(flightNumber, date);

        // Assert
        result.Should().NotBeNull();
        result!.FlightNumber.Should().Be(flightNumber);
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherAsync_WithCachedData_ReturnsCachedWeather()
    {
        // Arrange
        var airportCode = "ORD";
        var cachedWeather = new WeatherInfo
        {
            Location = "Chicago",
            Temperature = 68.0,
            Conditions = "Clear",
            Visibility = 10.0,
            WindSpeed = 5.0,
            WindDirection = "NW"
        };

        var cachedJson = JsonSerializer.Serialize(cachedWeather);
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Encoding.UTF8.GetBytes(cachedJson));

        // Act
        var result = await _flightService.GetWeatherAsync(airportCode);

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().Be("Chicago");
        result.Temperature.Should().Be(68.0);
        _mockCache.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncFlightDataFromExternalApiAsync_WithNewFlights_AddsFlightsToDatabase()
    {
        // Arrange
        var airportCode = "ORD";
        var externalFlights = new List<ExternalFlightData>
        {
            new ExternalFlightData
            {
                FlightNumber = "AA123",
                Airline = "American Airlines",
                OriginAirport = "ORD",
                DestinationAirport = "LAX",
                ScheduledDeparture = DateTime.Today.AddHours(10),
                ScheduledArrival = DateTime.Today.AddHours(14),
                Status = "OnTime"
            }
        };

        _mockFlightDataService.Setup(x => x.GetFlightDataAsync(airportCode))
                             .ReturnsAsync(externalFlights);

        // Act
        await _flightService.SyncFlightDataFromExternalApiAsync(airportCode);

        // Assert
        var flights = await _context.Flights.ToListAsync();
        flights.Should().HaveCount(1);
        flights.First().FlightNumber.Should().Be("AA123");
        flights.First().Status.Should().Be(FlightStatus.OnTime);
    }

    [Fact]
    public async Task GetFlightBoardAsync_WhenCacheThrowsException_FallsBackToDatabase()
    {
        // Arrange
        var airportCode = "ORD";
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = DateTime.Today.AddHours(10),
            Status = FlightStatus.OnTime
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("Cache error"));

        // Act
        var result = await _flightService.GetFlightBoardAsync(airportCode);

        // Assert
        result.Should().HaveCount(1);
        result.First().FlightNumber.Should().Be("AA123");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}