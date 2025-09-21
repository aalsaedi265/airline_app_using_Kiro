# Database Migrations

This directory contains Entity Framework migrations for the airline simulation application. The migrations have been created and are ready to use.

## Current Migration Structure

The following migration files are already generated and available:

### 1. `{timestamp}_InitialCreate.cs`
Contains the migration logic to create all tables:

- AspNetUsers, AspNetRoles, AspNetUserRoles (Identity tables)
- Flights table with indexes
- Bookings table with foreign keys and unique constraints
- Passengers table with foreign key to Bookings
- NotificationPreferences table with one-to-one relationship to Users
- BaggageItems table with foreign key to Bookings and unique tracking numbers
- LoyaltyAccounts table with one-to-one relationship to Users

### 2. `{timestamp}_InitialCreate.Designer.cs`
Contains the model snapshot for Entity Framework

### 3. `ApplicationDbContextModelSnapshot.cs`
Contains the current model state for future migrations

## Key Database Features

The migration will create:

**Tables with Proper Relationships:**
- Foreign key constraints with appropriate delete behaviors
- One-to-many relationships (User -> Bookings, Flight -> Bookings, Booking -> Passengers)
- One-to-one relationships (User -> NotificationPreferences, User -> LoyaltyAccount)

**Indexes for Performance:**
- Flight.FlightNumber
- Flight.(OriginAirport, ScheduledDeparture) - composite index
- Booking.ConfirmationNumber (unique)
- BaggageItem.TrackingNumber (unique)
- LoyaltyAccount.MembershipNumber (unique)

**Data Types:**
- Decimal columns with proper precision for monetary values
- String columns with appropriate max lengths
- Enum columns stored as strings for readability
- DateTime columns for timestamps

**Constraints:**
- Required field validations
- String length constraints
- Unique constraints where needed
- Range validations for numeric fields

## Using the Migrations

To apply the migrations to your database:
```bash
dotnet ef database update
```

To create a new migration after making model changes:
```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

## Simplified Architecture Notes

This application uses a simplified architecture that focuses on core functionality:

- **No Redis**: Uses IMemoryCache for simpler caching
- **No Hangfire**: Uses BackgroundService for background jobs
- **No External APIs**: Uses mock data services for flight and weather data
- **JWT Authentication**: Simple token-based auth instead of full Identity
- **PostgreSQL**: Single database for all data storage

The migration structure supports all these features while maintaining simplicity and ease of development.