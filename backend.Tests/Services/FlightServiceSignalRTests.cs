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
using Xunit;

namespace AirlineSimulationApi.Tests.Services;

public class FlightServiceSignalRTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IFlightDataService> _mockFlightDataService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IHubContext<FlightUpdatesHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<ILogger<FlightService>> _mockLogger;
    private readonly FlightService _flightService;

    public FlightServiceSignalRTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockFlightDataService = new Mock<IFlightDataService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _mockCache = new Mock<IDistributedCache>();
        _mockHubContext = new Mock<IHubContext<FlightUpdatesHub>>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockClients = new Mock<IHubClients>();
        _mockLogger = new Mock<ILogger<FlightService>>();

        // Setup SignalR mocks - don't mock SendAsync as it's an extension method
        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

        _flightService = new FlightService(
            _context,
            _mockFlightDataService.Object,
            _mockWeatherService.Object,
            _mockCache.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateFlightStatusAsync_WithSignalRContext_UpdatesFlightSuccessfully()
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = DateTime.Today.AddHours(10),
            ScheduledArrival = DateTime.Today.AddHours(14),
            Status = FlightStatus.OnTime,
            Gate = "B12",
            Terminal = "1"
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        // Act
        await _flightService.UpdateFlightStatusAsync("AA123", FlightStatus.Delayed);

        // Assert
        var updatedFlight = await _context.Flights.FirstAsync(f => f.FlightNumber == "AA123");
        updatedFlight.Status.Should().Be(FlightStatus.Delayed);
        
        // Verify SignalR context was accessed (we can't easily mock SendAsync extension method)
        _mockHubContext.Verify(x => x.Clients, Times.AtLeastOnce);
        _mockClients.Verify(x => x.Group(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateFlightGateAsync_WithSignalRContext_UpdatesGateSuccessfully()
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = DateTime.Today.AddHours(10),
            Status = FlightStatus.OnTime,
            Gate = "B12",
            Terminal = "1"
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        // Act
        await _flightService.UpdateFlightGateAsync("AA123", "C15", "2");

        // Assert
        var updatedFlight = await _context.Flights.FirstAsync(f => f.FlightNumber == "AA123");
        updatedFlight.Gate.Should().Be("C15");
        updatedFlight.Terminal.Should().Be("2");
        
        // Verify SignalR context was accessed for multiple groups
        _mockHubContext.Verify(x => x.Clients, Times.AtLeastOnce);
        _mockClients.Verify(x => x.Group(It.IsAny<string>()), Times.AtLeast(3)); // flight + 2 airports
    }

    [Fact]
    public async Task UpdateFlightDelayAsync_WithSignalRContext_UpdatesDelaySuccessfully()
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = DateTime.Today.AddHours(10),
            ScheduledArrival = DateTime.Today.AddHours(14),
            Status = FlightStatus.OnTime
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        var newDeparture = DateTime.Today.AddHours(10).AddMinutes(30);
        var newArrival = DateTime.Today.AddHours(14).AddMinutes(30);

        // Act
        await _flightService.UpdateFlightDelayAsync("AA123", newDeparture, newArrival, "Weather delay");

        // Assert
        var updatedFlight = await _context.Flights.FirstAsync(f => f.FlightNumber == "AA123");
        updatedFlight.Status.Should().Be(FlightStatus.Delayed);
        updatedFlight.EstimatedDeparture.Should().Be(newDeparture);
        updatedFlight.EstimatedArrival.Should().Be(newArrival);
        
        // Verify SignalR context was accessed
        _mockHubContext.Verify(x => x.Clients, Times.AtLeastOnce);
        _mockClients.Verify(x => x.Group(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateFlightStatusAsync_ToCancelled_UpdatesStatusSuccessfully()
    {
        // Arrange
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

        // Act
        await _flightService.UpdateFlightStatusAsync("AA123", FlightStatus.Cancelled);

        // Assert
        var updatedFlight = await _context.Flights.FirstAsync(f => f.FlightNumber == "AA123");
        updatedFlight.Status.Should().Be(FlightStatus.Cancelled);
        
        // Verify SignalR context was accessed (should send both status change and cancellation updates)
        _mockHubContext.Verify(x => x.Clients, Times.AtLeastOnce);
        _mockClients.Verify(x => x.Group(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateFlightStatusAsync_NonexistentFlight_DoesNotAccessSignalR()
    {
        // Act
        await _flightService.UpdateFlightStatusAsync("NONEXISTENT", FlightStatus.Delayed);

        // Assert - SignalR should not be accessed for non-existent flights
        _mockHubContext.Verify(x => x.Clients, Times.Never);
    }

    [Fact]
    public async Task FlightUpdates_AccessSignalRGroupsCorrectly()
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "aa123", // lowercase to test normalization
            Airline = "American Airlines",
            OriginAirport = "ord", // lowercase to test normalization
            DestinationAirport = "lax", // lowercase to test normalization
            ScheduledDeparture = DateTime.Today.AddHours(10),
            Status = FlightStatus.OnTime
        };

        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();

        // Act
        await _flightService.UpdateFlightStatusAsync("aa123", FlightStatus.Delayed);

        // Assert - Verify the correct groups are accessed (case-insensitive)
        _mockClients.Verify(x => x.Group("flight_AA123"), Times.Once);
        _mockClients.Verify(x => x.Group("airport_ORD"), Times.Once);
        _mockClients.Verify(x => x.Group("airport_LAX"), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}