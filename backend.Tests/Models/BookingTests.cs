using AirlineSimulationApi.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AirlineSimulationApi.Tests.Models;

public class BookingTests
{
    [Fact]
    public void Booking_ShouldHaveValidProperties()
    {
        // Arrange & Act
        var booking = new Booking
        {
            ConfirmationNumber = "ABC123",
            UserId = "user-123",
            FlightId = 1,
            Status = BookingStatus.Confirmed,
            TotalAmount = 299.99m,
            PaymentStatus = PaymentStatus.Completed
        };

        // Assert
        booking.ConfirmationNumber.Should().Be("ABC123");
        booking.UserId.Should().Be("user-123");
        booking.FlightId.Should().Be(1);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.TotalAmount.Should().Be(299.99m);
        booking.PaymentStatus.Should().Be(PaymentStatus.Completed);
        booking.Passengers.Should().NotBeNull();
        booking.BaggageItems.Should().NotBeNull();
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", false)] // Empty confirmation number
    [InlineData("TOOLONG", false)] // Too long confirmation number
    [InlineData("ABC123", true)] // Valid confirmation number
    public void Booking_ConfirmationNumber_ShouldValidateCorrectly(string confirmationNumber, bool isValid)
    {
        // Arrange
        var booking = new Booking
        {
            ConfirmationNumber = confirmationNumber,
            UserId = "user-123",
            FlightId = 1,
            Status = BookingStatus.Confirmed,
            TotalAmount = 299.99m,
            PaymentStatus = PaymentStatus.Completed
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(booking);
        var actualIsValid = Validator.TryValidateObject(booking, validationContext, validationResults, true);

        // Assert
        actualIsValid.Should().Be(isValid);
        if (!isValid)
        {
            validationResults.Should().NotBeEmpty();
        }
    }

    [Theory]
    [InlineData(-1, false)] // Negative amount
    [InlineData(1000000, false)] // Too large amount
    [InlineData(299.99, true)] // Valid amount
    [InlineData(0, true)] // Zero amount (valid for free flights)
    public void Booking_TotalAmount_ShouldValidateCorrectly(decimal amount, bool isValid)
    {
        // Arrange
        var booking = new Booking
        {
            ConfirmationNumber = "ABC123",
            UserId = "user-123",
            FlightId = 1,
            Status = BookingStatus.Confirmed,
            TotalAmount = amount,
            PaymentStatus = PaymentStatus.Completed
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(booking);
        var actualIsValid = Validator.TryValidateObject(booking, validationContext, validationResults, true);

        // Assert
        actualIsValid.Should().Be(isValid);
    }

    [Fact]
    public void Booking_ShouldInitializeCollections()
    {
        // Arrange & Act
        var booking = new Booking();

        // Assert
        booking.Passengers.Should().NotBeNull().And.BeEmpty();
        booking.BaggageItems.Should().NotBeNull().And.BeEmpty();
    }
}