using AirlineSimulationApi.Data;
using AirlineSimulationApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AirlineSimulationApi.Tests.Services;

public class FlightServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IFlightDataService> _mockFlightDataService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<ILogger<FlightService>> _mockLogger;
    private readonly FlightService _flightService;

    public FlightServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockFlightDataService = new Mock<IFlightDataService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _mockLogger = new Mock<ILogger<FlightService>>();

        _flightService = new FlightService(
            _context,
            _mockFlightDataService.Object,
            _mockWeatherService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetWeatherAsync_WithValidAirportCode_ReturnsWeatherInfo()
    {
        // Arrange
        var weatherData = new WeatherData
        {
            Location = "Chicago",
            Temperature = 20.0,
            Conditions = "Clear",
            Visibility = 10.0,
            WindSpeed = 5.0,
            WindDirectionText = "NW"
        };

        _mockWeatherService.Setup(x => x.GetWeatherAsync("ORD"))
            .ReturnsAsync(weatherData);

        // Act
        var result = await _flightService.GetWeatherAsync("ORD");

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().Be("Chicago");
        result.Temperature.Should().BeApproximately(68.0, 1.0); // Converted to Fahrenheit
        result.Conditions.Should().Be("Clear");
        result.Visibility.Should().Be(10.0);
        result.WindSpeed.Should().Be(5.0);
        result.WindDirection.Should().Be("NW");
    }

    [Fact]
    public async Task GetWeatherAsync_WhenWeatherServiceThrowsException_ReturnsNull()
    {
        // Arrange
        _mockWeatherService.Setup(x => x.GetWeatherAsync("ORD"))
            .ThrowsAsync(new WeatherServiceException("Service unavailable"));

        // Act
        var result = await _flightService.GetWeatherAsync("ORD");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_WhenWeatherServiceReturnsNull_ReturnsNull()
    {
        // Arrange
        _mockWeatherService.Setup(x => x.GetWeatherAsync("ORD"))
            .ReturnsAsync((WeatherData?)null);

        // Act
        var result = await _flightService.GetWeatherAsync("ORD");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_LogsWarningWhenNoWeatherData()
    {
        // Arrange
        _mockWeatherService.Setup(x => x.GetWeatherAsync("XXX"))
            .ReturnsAsync((WeatherData?)null);

        // Act
        await _flightService.GetWeatherAsync("XXX");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No weather data available for airport XXX")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWeatherAsync_LogsErrorWhenWeatherServiceThrowsException()
    {
        // Arrange
        var exception = new WeatherServiceException("Service error");
        _mockWeatherService.Setup(x => x.GetWeatherAsync("ORD"))
            .ThrowsAsync(exception);

        // Act
        await _flightService.GetWeatherAsync("ORD");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve weather data for airport ORD")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}