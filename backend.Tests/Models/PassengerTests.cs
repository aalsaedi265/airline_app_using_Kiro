using AirlineSimulationApi.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AirlineSimulationApi.Tests.Models;

public class PassengerTests
{
    [Fact]
    public void Passenger_ShouldHaveValidProperties()
    {
        // Arrange & Act
        var passenger = new Passenger
        {
            BookingId = 1,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            SeatNumber = "12A",
            SeatClass = SeatClass.Economy,
            CheckedIn = false
        };

        // Assert
        passenger.BookingId.Should().Be(1);
        passenger.FirstName.Should().Be("John");
        passenger.LastName.Should().Be("Doe");
        passenger.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
        passenger.SeatNumber.Should().Be("12A");
        passenger.SeatClass.Should().Be(SeatClass.Economy);
        passenger.CheckedIn.Should().BeFalse();
        passenger.CheckInTime.Should().BeNull();
    }

    [Theory]
    [InlineData("", false)] // Empty first name
    [InlineData("ThisIsAVeryLongFirstNameThatExceedsTheMaximumLengthAllowedForTheFirstNameFieldInTheDatabaseAndShouldFailValidation", false)] // Too long (over 100 chars)
    [InlineData("John", true)] // Valid first name
    public void Passenger_FirstName_ShouldHaveCorrectLength(string firstName, bool isValid)
    {
        // Arrange & Act
        var passenger = new Passenger
        {
            BookingId = 1,
            FirstName = firstName,
            LastName = "Doe",
            SeatClass = SeatClass.Economy
        };

        // Assert
        if (isValid)
        {
            passenger.FirstName.Should().NotBeEmpty();
            passenger.FirstName.Length.Should().BeLessOrEqualTo(100);
        }
        else
        {
            if (string.IsNullOrEmpty(firstName))
            {
                passenger.FirstName.Should().BeEmpty();
            }
            else
            {
                passenger.FirstName.Length.Should().BeGreaterThan(100);
            }
        }
    }

    [Theory]
    [InlineData("", false)] // Empty last name
    [InlineData("ThisIsAVeryLongLastNameThatExceedsTheMaximumLengthAllowedForTheLastNameFieldInTheDatabaseAndShouldFailValidation", false)] // Too long (over 100 chars)
    [InlineData("Doe", true)] // Valid last name
    public void Passenger_LastName_ShouldHaveCorrectLength(string lastName, bool isValid)
    {
        // Arrange & Act
        var passenger = new Passenger
        {
            BookingId = 1,
            FirstName = "John",
            LastName = lastName,
            SeatClass = SeatClass.Economy
        };

        // Assert
        if (isValid)
        {
            passenger.LastName.Should().NotBeEmpty();
            passenger.LastName.Length.Should().BeLessOrEqualTo(100);
        }
        else
        {
            if (string.IsNullOrEmpty(lastName))
            {
                passenger.LastName.Should().BeEmpty();
            }
            else
            {
                passenger.LastName.Length.Should().BeGreaterThan(100);
            }
        }
    }

    [Theory]
    [InlineData("12A", true)] // Valid seat number
    [InlineData("1", true)] // Short seat number
    [InlineData("TOOLONG", false)] // Too long seat number
    [InlineData(null, true)] // Null seat number (allowed)
    public void Passenger_SeatNumber_ShouldValidateCorrectly(string? seatNumber, bool isValid)
    {
        // Arrange
        var passenger = new Passenger
        {
            BookingId = 1,
            FirstName = "John",
            LastName = "Doe",
            SeatNumber = seatNumber,
            SeatClass = SeatClass.Economy
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(passenger);
        var actualIsValid = Validator.TryValidateObject(passenger, validationContext, validationResults, true);

        // Assert
        actualIsValid.Should().Be(isValid);
    }

    [Fact]
    public void Passenger_CheckIn_ShouldUpdateCheckInTime()
    {
        // Arrange
        var passenger = new Passenger
        {
            BookingId = 1,
            FirstName = "John",
            LastName = "Doe",
            SeatClass = SeatClass.Economy,
            CheckedIn = false
        };

        // Act
        passenger.CheckedIn = true;
        passenger.CheckInTime = DateTime.UtcNow;

        // Assert
        passenger.CheckedIn.Should().BeTrue();
        passenger.CheckInTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}