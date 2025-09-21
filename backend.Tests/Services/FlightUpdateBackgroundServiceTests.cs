using AirlineSimulationApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AirlineSimulationApi.Tests.Services;

public class FlightUpdateBackgroundServiceTests
{
    private readonly Mock<IFlightService> _mockFlightService;
    private readonly Mock<ILogger<FlightUpdateBackgroundService>> _mockLogger;
    private readonly FlightUpdateBackgroundService _backgroundService;

    public FlightUpdateBackgroundServiceTests()
    {
        _mockFlightService = new Mock<IFlightService>();
        _mockLogger = new Mock<ILogger<FlightUpdateBackgroundService>>();
        _backgroundService = new FlightUpdateBackgroundService(_mockFlightService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateFlightDataAsync_ValidAirportCode_CallsFlightServiceSync()
    {
        // Arrange
        var airportCode = "ORD";
        _mockFlightService.Setup(x => x.SyncFlightDataFromExternalApiAsync(airportCode))
                         .Returns(Task.CompletedTask);

        // Act
        await _backgroundService.UpdateFlightDataAsync(airportCode);

        // Assert
        _mockFlightService.Verify(x => x.SyncFlightDataFromExternalApiAsync(airportCode), Times.Once);
    }

    [Fact]
    public async Task UpdateFlightDataAsync_WhenFlightServiceThrows_RethrowsException()
    {
        // Arrange
        var airportCode = "ORD";
        var exception = new Exception("Service error");
        _mockFlightService.Setup(x => x.SyncFlightDataFromExternalApiAsync(airportCode))
                         .ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => 
            _backgroundService.UpdateFlightDataAsync(airportCode));
        
        thrownException.Should().Be(exception);
        _mockFlightService.Verify(x => x.SyncFlightDataFromExternalApiAsync(airportCode), Times.Once);
    }

    [Fact]
    public async Task UpdateAllAirportsAsync_CallsFlightServiceForAllAirports()
    {
        // Arrange
        var expectedAirports = new[] { "ORD", "LAX", "JFK", "ATL", "DFW", "DEN", "SFO", "SEA", "LAS", "PHX" };
        
        foreach (var airport in expectedAirports)
        {
            _mockFlightService.Setup(x => x.SyncFlightDataFromExternalApiAsync(airport))
                             .Returns(Task.CompletedTask);
        }

        // Act
        await _backgroundService.UpdateAllAirportsAsync();

        // Assert
        foreach (var airport in expectedAirports)
        {
            _mockFlightService.Verify(x => x.SyncFlightDataFromExternalApiAsync(airport), Times.Once);
        }
    }

    [Fact]
    public async Task UpdateAllAirportsAsync_WhenOneAirportFails_RethrowsException()
    {
        // Arrange
        var exception = new Exception("Service error");
        _mockFlightService.Setup(x => x.SyncFlightDataFromExternalApiAsync("ORD"))
                         .ThrowsAsync(exception);
        
        _mockFlightService.Setup(x => x.SyncFlightDataFromExternalApiAsync("LAX"))
                         .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _backgroundService.UpdateAllAirportsAsync());
    }
}