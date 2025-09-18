# Backend Setup Instructions

## Prerequisites

### Install .NET 8 SDK

The .NET 8 SDK is required to build and run this application. 

**Windows:**
1. Download .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer and follow the installation wizard
3. Verify installation by opening a new command prompt and running: `dotnet --version`

**Alternative installation methods:**
- Using Chocolatey: `choco install dotnet-8.0-sdk`
- Using winget: `winget install Microsoft.DotNet.SDK.8`

## Database Setup

Once .NET SDK is installed, follow these steps to set up the database:

### 1. Install Entity Framework Tools
```bash
dotnet tool install --global dotnet-ef
```

### 2. Create Initial Migration
```bash
cd backend
dotnet ef migrations add InitialCreate
```

### 3. Update Database
```bash
dotnet ef database update
```

### 4. Verify Migration
The migration should create the following tables:
- AspNetUsers (Identity tables)
- Flights
- Bookings
- Passengers
- NotificationPreferences
- BaggageItems
- LoyaltyAccounts

## Running Tests

### Install Test Dependencies
```bash
cd backend.Tests
dotnet restore
```

### Run Unit Tests
```bash
dotnet test
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Database Configuration

The application uses PostgreSQL by default. Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AirlineSimulationDb;Username=your_username;Password=your_password"
  }
}
```

## Entity Framework Commands

### Add New Migration
```bash
dotnet ef migrations add <MigrationName>
```

### Remove Last Migration
```bash
dotnet ef migrations remove
```

### Update Database to Specific Migration
```bash
dotnet ef database update <MigrationName>
```

### Generate SQL Script
```bash
dotnet ef migrations script
```

## Model Validation

All entity models include comprehensive validation attributes:

- **Flight**: FlightNumber (max 10 chars), Airline (max 50 chars), Airport codes (3 chars)
- **Booking**: ConfirmationNumber (6 chars), TotalAmount (decimal with range validation)
- **Passenger**: FirstName/LastName (max 100 chars), SeatNumber (max 5 chars)
- **ApplicationUser**: FirstName/LastName (max 100 chars)

## Database Indexes

The following indexes are configured for optimal performance:

- Flight: FlightNumber, (OriginAirport, ScheduledDeparture)
- Booking: ConfirmationNumber (unique)
- BaggageItem: TrackingNumber (unique)
- LoyaltyAccount: MembershipNumber (unique)