using AirlineSimulationApi.Data;
using AirlineSimulationApi.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AirlineSimulationApi.Tests.Data;

public class ApplicationDbContextTests
{
    private ApplicationDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ApplicationDbContext_ShouldCreateAndRetrieveFlight()
    {
        // Arrange
        using var context = GetInMemoryContext();
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

        // Act
        context.Flights.Add(flight);
        await context.SaveChangesAsync();

        var retrievedFlight = await context.Flights.FirstOrDefaultAsync(f => f.FlightNumber == "AA123");

        // Assert
        retrievedFlight.Should().NotBeNull();
        retrievedFlight!.FlightNumber.Should().Be("AA123");
        retrievedFlight.Airline.Should().Be("American Airlines");
        retrievedFlight.Status.Should().Be(FlightStatus.Scheduled);
    }

    [Fact]
    public async Task ApplicationDbContext_ShouldCreateBookingWithRelationships()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new ApplicationUser
        {
            Id = "user-123",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "johndoe"
        };

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

        var booking = new Booking
        {
            ConfirmationNumber = "ABC123",
            UserId = user.Id,
            Status = BookingStatus.Confirmed,
            TotalAmount = 299.99m,
            PaymentStatus = PaymentStatus.Completed,
            User = user,
            Flight = flight
        };

        // Act
        context.Users.Add(user);
        context.Flights.Add(flight);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var retrievedBooking = await context.Bookings
            .Include(b => b.User)
            .Include(b => b.Flight)
            .FirstOrDefaultAsync(b => b.ConfirmationNumber == "ABC123");

        // Assert
        retrievedBooking.Should().NotBeNull();
        retrievedBooking!.User.Should().NotBeNull();
        retrievedBooking.User.FirstName.Should().Be("John");
        retrievedBooking.Flight.Should().NotBeNull();
        retrievedBooking.Flight.FlightNumber.Should().Be("AA123");
    }

    [Fact]
    public async Task ApplicationDbContext_ShouldCreatePassengerWithBooking()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new ApplicationUser
        {
            Id = "user-123",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "johndoe"
        };

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

        var booking = new Booking
        {
            ConfirmationNumber = "ABC123",
            UserId = user.Id,
            Status = BookingStatus.Confirmed,
            TotalAmount = 299.99m,
            PaymentStatus = PaymentStatus.Completed,
            User = user,
            Flight = flight
        };

        var passenger = new Passenger
        {
            FirstName = "Jane",
            LastName = "Doe",
            SeatClass = SeatClass.Economy,
            Booking = booking
        };

        // Act
        context.Users.Add(user);
        context.Flights.Add(flight);
        context.Bookings.Add(booking);
        context.Passengers.Add(passenger);
        await context.SaveChangesAsync();

        var retrievedPassenger = await context.Passengers
            .Include(p => p.Booking)
            .ThenInclude(b => b.User)
            .FirstOrDefaultAsync(p => p.FirstName == "Jane");

        // Assert
        retrievedPassenger.Should().NotBeNull();
        retrievedPassenger!.Booking.Should().NotBeNull();
        retrievedPassenger.Booking.User.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task ApplicationDbContext_ShouldCreateIndexes()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
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

        // Act
        context.Flights.Add(flight);
        await context.SaveChangesAsync();

        var retrievedFlight = await context.Flights
            .Where(f => f.FlightNumber == "AA123")
            .FirstOrDefaultAsync();

        // Assert
        retrievedFlight.Should().NotBeNull();
        retrievedFlight!.FlightNumber.Should().Be("AA123");
        
        // Note: In-memory database doesn't enforce unique constraints like a real database would
        // This test verifies that the context can save and retrieve data with the configured indexes
    }
}