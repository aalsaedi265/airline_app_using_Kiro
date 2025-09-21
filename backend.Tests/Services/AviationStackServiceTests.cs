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

public class AviationStackServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AviationStackService>> _loggerMock;
    private readonly AviationStackService _service;

    public AviationStackServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.aviationstack.com/v1/")
        };
        
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["AviationStack:ApiKey"]).Returns("test-api-key");
        
        _loggerMock = new Mock<ILogger<AviationStackService>>();
        
        _service = new AviationStackService(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetFlightDataAsync_ValidResponse_ReturnsFlightData()
    {
        // Arrange
        var mockResponse = new AviationStackResponse
        {
            Data = new[]
            {
                new AviationStackFlight
                {
                    Flight = new AviationStackFlightInfo { Iata = "AA123" },
                    Airline = new AviationStackAirline { Name = "American Airlines", Iata = "AA" },
                    Departure = new AviationStackAirport 
                    { 
                        Iata = "ORD", 
                        Scheduled = "2024-01-15T10:00:00Z",
                        Gate = "B12",
                        Terminal = "3"
                    },
                    Arrival = new AviationStackAirport 
                    { 
                        Iata = "LAX", 
                        Scheduled = "2024-01-15T13:00:00Z" 
                    },
                    FlightStatus = "active"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetFlightDataAsync("ORD");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        
        var flight = result.First();
        flight.FlightNumber.Should().Be("AA123");
        flight.Airline.Should().Be("American Airlines");
        flight.AirlineIata.Should().Be("AA");
        flight.OriginAirport.Should().Be("ORD");
        flight.DestinationAirport.Should().Be("LAX");
        flight.Status.Should().Be("active");
        flight.Gate.Should().Be("B12");
        flight.Terminal.Should().Be("3");
    }

    [Fact]
    public async Task GetFlightDataAsync_ApiReturnsError_ThrowsFlightDataServiceException()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FlightDataServiceException>(
            () => _service.GetFlightDataAsync("ORD"));
        
        exception.Message.Should().Contain("AviationStack API returned BadRequest");
    }

    [Fact]
    public async Task GetFlightDataAsync_NetworkError_RetriesAndThrowsException()
    {
        // Arrange
        _httpMessageHandlerMock.SetupAnyRequest()
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FlightDataServiceException>(
            () => _service.GetFlightDataAsync("ORD"));
        
        exception.Message.Should().Contain("Failed to retrieve flight data from AviationStack");
        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task GetFlightDataAsync_EmptyResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var mockResponse = new AviationStackResponse { Data = Array.Empty<AviationStackFlight>() };
        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetFlightDataAsync("ORD");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFlightDataAsync_NullResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var mockResponse = new AviationStackResponse { Data = null };
        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetFlightDataAsync("ORD");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFlightDetailsAsync_ValidResponse_ReturnsFlightDetails()
    {
        // Arrange
        var mockResponse = new AviationStackResponse
        {
            Data = new[]
            {
                new AviationStackFlight
                {
                    Flight = new AviationStackFlightInfo { Iata = "AA123" },
                    Airline = new AviationStackAirline { Name = "American Airlines" },
                    Departure = new AviationStackAirport { Iata = "ORD", Scheduled = "2024-01-15T10:00:00Z" },
                    Arrival = new AviationStackAirport { Iata = "LAX", Scheduled = "2024-01-15T13:00:00Z" },
                    FlightStatus = "active"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetFlightDetailsAsync("AA123", DateTime.Today);

        // Assert
        result.Should().NotBeNull();
        result!.FlightNumber.Should().Be("AA123");
        result.Airline.Should().Be("American Airlines");
    }

    [Fact]
    public async Task GetFlightDetailsAsync_NoFlightFound_ReturnsNull()
    {
        // Arrange
        var mockResponse = new AviationStackResponse { Data = Array.Empty<AviationStackFlight>() };
        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        
        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse, "application/json");

        // Act
        var result = await _service.GetFlightDetailsAsync("XX999", DateTime.Today);

        // Assert
        result.Should().BeNull();
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
        configMock.Setup(x => x["AviationStack:ApiKey"]).Returns((string?)null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new AviationStackService(_httpClient, configMock.Object, _loggerMock.Object));
    }
}