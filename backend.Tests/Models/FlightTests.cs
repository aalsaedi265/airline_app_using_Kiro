using AirlineSimulationApi.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AirlineSimulationApi.Tests.Models;

public class FlightTests
{
    [Fact]
    public void Flight_ShouldHaveValidProperties()
    {
        // Arrange & Act
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "LAX",
            DestinationAirport = "JFK",
            ScheduledDeparture = DateTime.UtcNow.AddHours(2),
            ScheduledArrival = DateTime.UtcNow.AddHours(8),
            Status = FlightStatus.Scheduled
        };

        // Assert
        flight.FlightNumber.Should().Be("AA123");
        flight.Airline.Should().Be("American Airlines");
        flight.OriginAirport.Should().Be("LAX");
        flight.DestinationAirport.Should().Be("JFK");
        flight.Status.Should().Be(FlightStatus.Scheduled);
        flight.Bookings.Should().NotBeNull();
        flight.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", false)] // Empty flight number
    [InlineData("TOOLONGFLIGHT", false)] // Too long flight number
    [InlineData("AA123", true)] // Valid flight number
    public void Flight_FlightNumber_ShouldValidateCorrectly(string flightNumber, bool isValid)
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = flightNumber,
            Airline = "Test Airline",
            OriginAirport = "LAX",
            DestinationAirport = "JFK",
            ScheduledDeparture = DateTime.UtcNow.AddHours(2),
            ScheduledArrival = DateTime.UtcNow.AddHours(8),
            Status = FlightStatus.Scheduled
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(flight);
        var actualIsValid = Validator.TryValidateObject(flight, validationContext, validationResults, true);

        // Assert
        actualIsValid.Should().Be(isValid);
        if (!isValid)
        {
            validationResults.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData("", false)] // Empty airline
    [InlineData("This airline name is way too long to be valid and should fail validation", false)] // Too long
    [InlineData("American Airlines", true)] // Valid airline
    public void Flight_Airline_ShouldValidateCorrectly(string airline, bool isValid)
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = airline,
            OriginAirport = "LAX",
            DestinationAirport = "JFK",
            ScheduledDeparture = DateTime.UtcNow.AddHours(2),
            ScheduledArrival = DateTime.UtcNow.AddHours(8),
            Status = FlightStatus.Scheduled
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(flight);
        var actualIsValid = Validator.TryValidateObject(flight, validationContext, validationResults, true);

        // Assert
        actualIsValid.Should().Be(isValid);
    }

    [Theory]
    [InlineData("", false)] // Empty airport code
    [InlineData("TOOLONG", false)] // Too long airport code
    [InlineData("LAX", true)] // Valid airport code
    public void Flight_AirportCodes_ShouldValidateCorrectly(string airportCode, bool isValid)
    {
        // Arrange
        var flight = new Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = airportCode,
            DestinationAirport = "JFK",
            ScheduledDeparture = DateTime.UtcNow.AddHours(2),
            ScheduledArrival = DateTime.UtcNow.AddHours(8),
            Status = FlightStatus.Scheduled
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(flight);
        var actualIsValid = Validator.TryValidateObject(flight, validationContext, validationResults, true);

        // Assert
        actualIsValid.Should().Be(isValid);
    }

    [Fact]
    public void Flight_UpdatedAt_ShouldBeSetOnCreation()
    {
        // Arrange & Act
        var flight = new Flight();

        // Assert
        flight.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}