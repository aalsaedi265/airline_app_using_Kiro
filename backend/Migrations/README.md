# Database Migrations

This directory will contain Entity Framework migrations once .NET SDK is installed and migrations are created.

## Expected Migration Structure

When you run `dotnet ef migrations add InitialCreate`, the following files will be generated:

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

To create the actual migration files, install .NET 8 SDK and run:
```bash
dotnet ef migrations add InitialCreate
```