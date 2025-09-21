using System.Net;
using System.Text.Json;
using AirlineSimulationApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace AirlineSimulationApi.Tests.Services;

public class OpenWeatherMapServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<OpenWeatherMapService>> _loggerMock;
    private readonly OpenWeatherMapService _service;

    public OpenWeatherMapServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/")
        };
        
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["OpenWeatherMap:ApiKey"]).Returns("test-api-key");
        
        _loggerMock = new Mock<ILogger<OpenWeatherMapService>>();
        
        _service = new OpenWeatherMapService(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetWeatherAsync_ValidAirportCode_ReturnsWeatherData()
    {
        // Arrange
        var mockResponse = new OpenWeatherMapResponse
        {
            Name = "Chicago",
            Main = new OpenWeatherMapMain
            {
                Temp = 20.5,
                Humidity = 65,
                Pressure = 1013.25
            },
            Weather = new[]
            {
                new OpenWeatherMapWeather
                {
                    Main = "Clear",
                    Description = "clear sky"
                }
            },
            Wind = new OpenWeatherMapWind
            {
                Speed = 3.5,
                Deg = 270
            },
            Visibility = 10000
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetWeatherAsync("ORD");

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().Be("Chicago");
        result.Temperature.Should().Be(20.5);
        result.TemperatureFahrenheit.Should().BeApproximately(68.9, 0.1);
        result.Conditions.Should().Be("Clear");
        result.Description.Should().Be("clear sky");
        result.Humidity.Should().Be(65);
        result.Pressure.Should().Be(1013.25);
        result.Visibility.Should().Be(10); // Converted from meters to kilometers
        result.WindSpeed.Should().Be(3.5);
        result.WindDirection.Should().Be(270);
        result.WindDirectionText.Should().Be("W");
    }

    [Fact]
    public async Task GetWeatherAsync_UnknownAirportCode_ReturnsNull()
    {
        // Act
        var result = await _service.GetWeatherAsync("XXX");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ValidCity_ReturnsWeatherData()
    {
        // Arrange
        var mockResponse = new OpenWeatherMapResponse
        {
            Name = "Chicago",
            Main = new OpenWeatherMapMain { Temp = 15.0, Humidity = 70, Pressure = 1015 },
            Weather = new[] { new OpenWeatherMapWeather { Main = "Clouds", Description = "overcast clouds" } },
            Wind = new OpenWeatherMapWind { Speed = 2.1, Deg = 180 },
            Visibility = 8000
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetWeatherByCityAsync("Chicago");

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().Be("Chicago");
        result.Temperature.Should().Be(15.0);
        result.Conditions.Should().Be("Clouds");
        result.WindDirectionText.Should().Be("S");
    }

    [Fact]
    public async Task GetWeatherByCityAsync_CityNotFound_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var result = await _service.GetWeatherByCityAsync("NonExistentCity");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherByCityAsync_ApiError_ThrowsWeatherServiceException()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WeatherServiceException>(
            () => _service.GetWeatherByCityAsync("Chicago"));
        
        exception.Message.Should().Contain("OpenWeatherMap API returned InternalServerError");
    }

    [Fact]
    public async Task GetWeatherByCityAsync_NetworkError_ThrowsWeatherServiceException()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WeatherServiceException>(
            () => _service.GetWeatherByCityAsync("Chicago"));
        
        exception.Message.Should().Contain("Failed to retrieve weather data from OpenWeatherMap");
        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(45, "NE")]
    [InlineData(90, "E")]
    [InlineData(135, "SE")]
    [InlineData(180, "S")]
    [InlineData(225, "SW")]
    [InlineData(270, "W")]
    [InlineData(315, "NW")]
    [InlineData(360, "N")]
    public async Task GetWeatherByCityAsync_WindDirection_MapsCorrectly(int degrees, string expectedDirection)
    {
        // Arrange
        var mockResponse = new OpenWeatherMapResponse
        {
            Name = "Test City",
            Main = new OpenWeatherMapMain { Temp = 20, Humidity = 50, Pressure = 1013 },
            Weather = new[] { new OpenWeatherMapWeather { Main = "Clear", Description = "clear" } },
            Wind = new OpenWeatherMapWind { Speed = 5, Deg = degrees },
            Visibility = 10000
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetWeatherByCityAsync("Test City");

        // Assert
        result.Should().NotBeNull();
        result!.WindDirectionText.Should().Be(expectedDirection);
    }

    [Fact]
    public async Task IsServiceAvailableAsync_ServiceRespondsOk_ReturnsTrue()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, "{}", "application/json");

        // Act
        var result = await _service.IsServiceAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsServiceAvailableAsync_ServiceError_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _service.IsServiceAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsServiceAvailableAsync_NetworkError_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.IsServiceAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Constructor_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["OpenWeatherMap:ApiKey"]).Returns((string?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new OpenWeatherMapService(_httpClient, configMock.Object, _loggerMock.Object));
    }

    [Fact]
    public async Task GetWeatherByCityAsync_NullResponse_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, "null", "application/json");

        // Act
        var result = await _service.GetWeatherByCityAsync("Chicago");

        // Assert
        result.Should().BeNull();
    }
}