using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using AirlineSimulationApi.Controllers;
using AirlineSimulationApi.Services;
using AirlineSimulationApi.Models;
using System.Net;

namespace AirlineSimulationApi.Tests.Controllers;

public class FlightsControllerTests
{
    private readonly Mock<IFlightService> _mockFlightService;
    private readonly Mock<ILogger<FlightsController>> _mockLogger;
    private readonly FlightsController _controller;

    public FlightsControllerTests()
    {
        _mockFlightService = new Mock<IFlightService>();
        _mockLogger = new Mock<ILogger<FlightsController>>();
        _controller = new FlightsController(_mockFlightService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetFlightBoard_WithValidAirport_ReturnsFlightData()
    {
        // Arrange
        var flights = new List<Flight>
        {
            new Flight
            {
                Id = 1,
                FlightNumber = "AA123",
                Airline = "American Airlines",
                OriginAirport = "ORD",
                DestinationAirport = "LAX",
                ScheduledDeparture = DateTime.Today.AddHours(10),
                EstimatedDeparture = DateTime.Today.AddHours(10).AddMinutes(15),
                ScheduledArrival = DateTime.Today.AddHours(13),
                EstimatedArrival = DateTime.Today.AddHours(13).AddMinutes(15),
                Status = FlightStatus.OnTime,
                Gate = "B12",
                Terminal = "3"
            }
        };

        _mockFlightService.Setup(x => x.GetFlightBoardAsync("ORD"))
            .ReturnsAsync(flights);

        // Act
        var result = await _controller.GetFlightBoard("ORD");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlightBoardResponse>().Subject;
        
        response.Airport.Should().Be("ORD");
        response.Flights.Should().HaveCount(1);
        response.Flights.First().FlightNumber.Should().Be("AA123");
        response.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFlightBoard_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        var flights = new List<Flight>
        {
            new Flight
            {
                Id = 1,
                FlightNumber = "AA123",
                Airline = "American Airlines",
                OriginAirport = "ORD",
                DestinationAirport = "LAX",
                Status = FlightStatus.OnTime
            },
            new Flight
            {
                Id = 2,
                FlightNumber = "UA456",
                Airline = "United Airlines",
                OriginAirport = "ORD",
                DestinationAirport = "SFO",
                Status = FlightStatus.Delayed
            }
        };

        _mockFlightService.Setup(x => x.GetFlightBoardAsync("ORD"))
            .ReturnsAsync(flights);

        // Act
        var result = await _controller.GetFlightBoard("ORD", "AA123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlightBoardResponse>().Subject;
        
        response.Flights.Should().HaveCount(1);
        response.Flights.First().FlightNumber.Should().Be("AA123");
    }

    [Fact]
    public async Task GetFlightBoard_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var flights = new List<Flight>
        {
            new Flight
            {
                Id = 1,
                FlightNumber = "AA123",
                Airline = "American Airlines",
                Status = FlightStatus.OnTime
            },
            new Flight
            {
                Id = 2,
                FlightNumber = "UA456",
                Airline = "United Airlines",
                Status = FlightStatus.Delayed
            }
        };

        _mockFlightService.Setup(x => x.GetFlightBoardAsync("ORD"))
            .ReturnsAsync(flights);

        // Act
        var result = await _controller.GetFlightBoard("ORD", status: FlightStatus.OnTime);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlightBoardResponse>().Subject;
        
        response.Flights.Should().HaveCount(1);
        response.Flights.First().Status.Should().Be(FlightStatus.OnTime);
    }

    [Fact]
    public async Task GetFlightBoard_WithInvalidAirportCode_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetFlightBoard("INVALID");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        
        errorResponse.Message.Should().Contain("Airport code must be a valid 3-letter IATA code");
    }

    [Fact]
    public async Task GetFlightBoard_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _mockFlightService.Setup(x => x.GetFlightBoardAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetFlightBoard("ORD");

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        
        var errorResponse = statusCodeResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("An error occurred while retrieving flight information");
    }

    [Fact]
    public async Task GetFlightDetails_WithValidFlightNumber_ReturnsFlightDetails()
    {
        // Arrange
        var flight = new Flight
        {
            Id = 1,
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            Status = FlightStatus.OnTime,
            Gate = "B12",
            Terminal = "3",
            Aircraft = "Boeing 737-800"
        };

        _mockFlightService.Setup(x => x.GetFlightDetailsAsync("AA123", It.IsAny<DateTime>()))
            .ReturnsAsync(flight);

        // Act
        var result = await _controller.GetFlightDetails("AA123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlightDetailsResponse>().Subject;
        
        response.Flight.FlightNumber.Should().Be("AA123");
        response.Flight.Airline.Should().Be("American Airlines");
        response.Flight.Aircraft.Should().Be("Boeing 737-800");
    }

    [Fact]
    public async Task GetFlightDetails_WithInvalidFlightNumber_ReturnsNotFound()
    {
        // Arrange
        _mockFlightService.Setup(x => x.GetFlightDetailsAsync("INVALID123", It.IsAny<DateTime>()))
            .ReturnsAsync((Flight?)null);

        // Act
        var result = await _controller.GetFlightDetails("INVALID123");

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        
        errorResponse.Message.Should().Contain("Flight INVALID123 not found");
    }

    [Fact]
    public async Task GetFlightDetails_WithEmptyFlightNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetFlightDetails("");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        
        errorResponse.Message.Should().Contain("Flight number is required");
    }

    [Fact]
    public async Task GetFlightWeather_WithValidFlightNumber_ReturnsWeatherData()
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "AA123",
            OriginAirport = "ORD",
            DestinationAirport = "LAX"
        };

        var originWeather = new WeatherInfo
        {
            Location = "Chicago",
            Temperature = 75.0,
            Conditions = "Clear",
            Visibility = 10.0,
            WindSpeed = 5.0,
            WindDirection = "NW"
        };

        var destinationWeather = new WeatherInfo
        {
            Location = "Los Angeles",
            Temperature = 80.0,
            Conditions = "Sunny",
            Visibility = 10.0,
            WindSpeed = 3.0,
            WindDirection = "SW"
        };

        _mockFlightService.Setup(x => x.GetFlightDetailsAsync("AA123", It.IsAny<DateTime>()))
            .ReturnsAsync(flight);
        _mockFlightService.Setup(x => x.GetWeatherAsync("ORD"))
            .ReturnsAsync(originWeather);
        _mockFlightService.Setup(x => x.GetWeatherAsync("LAX"))
            .ReturnsAsync(destinationWeather);

        // Act
        var result = await _controller.GetFlightWeather("AA123");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<WeatherResponse>().Subject;
        
        response.FlightNumber.Should().Be("AA123");
        response.OriginAirport.Should().Be("ORD");
        response.DestinationAirport.Should().Be("LAX");
        response.OriginWeather.Should().NotBeNull();
        response.DestinationWeather.Should().NotBeNull();
        response.OriginWeather!.Location.Should().Be("Chicago");
        response.DestinationWeather!.Location.Should().Be("Los Angeles");
    }

    [Fact]
    public async Task GetFlightWeather_WithInvalidFlightNumber_ReturnsNotFound()
    {
        // Arrange
        _mockFlightService.Setup(x => x.GetFlightDetailsAsync("INVALID123", It.IsAny<DateTime>()))
            .ReturnsAsync((Flight?)null);

        // Act
        var result = await _controller.GetFlightWeather("INVALID123");

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        
        errorResponse.Message.Should().Contain("Flight INVALID123 not found");
    }

    [Fact]
    public async Task GetFlightWeather_WithEmptyFlightNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetFlightWeather("");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        
        errorResponse.Message.Should().Contain("Flight number is required");
    }

    [Fact]
    public async Task GetFlightBoard_WithMultipleFilters_ReturnsCorrectlyFilteredResults()
    {
        // Arrange
        var flights = new List<Flight>
        {
            new Flight
            {
                Id = 1,
                FlightNumber = "AA123",
                Airline = "American Airlines",
                Status = FlightStatus.OnTime
            },
            new Flight
            {
                Id = 2,
                FlightNumber = "UA456",
                Airline = "United Airlines",
                Status = FlightStatus.OnTime
            },
            new Flight
            {
                Id = 3,
                FlightNumber = "AA789",
                Airline = "American Airlines",
                Status = FlightStatus.Delayed
            }
        };

        _mockFlightService.Setup(x => x.GetFlightBoardAsync("ORD"))
            .ReturnsAsync(flights);

        // Act
        var result = await _controller.GetFlightBoard("ORD", "American", FlightStatus.OnTime);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlightBoardResponse>().Subject;
        
        response.Flights.Should().HaveCount(1);
        response.Flights.First().FlightNumber.Should().Be("AA123");
        response.Flights.First().Status.Should().Be(FlightStatus.OnTime);
        response.Flights.First().Airline.Should().Contain("American");
    }
}