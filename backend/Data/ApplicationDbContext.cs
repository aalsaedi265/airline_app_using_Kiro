using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Flight> Flights { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Passenger> Passengers { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<BaggageItem> BaggageItems { get; set; }
    public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Flight entity configuration
        builder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FlightNumber);
            entity.HasIndex(e => new { e.OriginAirport, e.ScheduledDeparture });
            entity.Property(e => e.FlightNumber).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Airline).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OriginAirport).HasMaxLength(3).IsRequired();
            entity.Property(e => e.DestinationAirport).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.Terminal).HasMaxLength(5);
            entity.Property(e => e.Aircraft).HasMaxLength(50);
        });

        // Booking entity configuration
        builder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConfirmationNumber).IsUnique();
            entity.Property(e => e.ConfirmationNumber).HasMaxLength(6).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
                  
            entity.HasOne(e => e.Flight)
                  .WithMany()
                  .HasForeignKey(e => e.FlightId);
                  
            entity.HasMany(e => e.Passengers)
                  .WithOne(e => e.Booking)
                  .HasForeignKey(e => e.BookingId);
        });

        // Passenger entity configuration
        builder.Entity<Passenger>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SeatNumber).HasMaxLength(5);
            entity.Property(e => e.SeatClass).HasMaxLength(20).IsRequired();
        });

        // NotificationPreferences entity configuration
        builder.Entity<NotificationPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne(e => e.NotificationPreferences)
                  .HasForeignKey<NotificationPreferences>(e => e.UserId);
        });

        // BaggageItem entity configuration
        builder.Entity<BaggageItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TrackingNumber).IsUnique();
            entity.Property(e => e.TrackingNumber).HasMaxLength(12).IsRequired();
            entity.HasOne(e => e.Booking)
                  .WithMany(e => e.BaggageItems)
                  .HasForeignKey(e => e.BookingId);
        });

        // LoyaltyAccount entity configuration
        builder.Entity<LoyaltyAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne(e => e.LoyaltyAccount)
                  .HasForeignKey<LoyaltyAccount>(e => e.UserId);
        });
    }
}