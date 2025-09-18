# Airline Simulation Application

A comprehensive airline simulation web application built with ASP.NET Core and React TypeScript, featuring real-time flight tracking, booking management, and premium airline services.

## Project Structure

```
airline-simulation-app/
├── backend/                 # ASP.NET Core Web API
│   ├── Controllers/         # API Controllers
│   ├── Services/           # Business Logic Services
│   ├── Models/             # Data Models
│   ├── Data/               # Entity Framework DbContext
│   └── Hubs/               # SignalR Hubs
├── frontend/               # React TypeScript SPA
│   ├── src/
│   │   ├── components/     # React Components
│   │   ├── pages/          # Page Components
│   │   ├── contexts/       # React Contexts
│   │   └── services/       # API Services
└── docker-compose.yml      # Docker configuration
```

## Technology Stack

### Backend
- ASP.NET Core 8.0 Web API
- Entity Framework Core with PostgreSQL
- SignalR for real-time communication
- Hangfire for background jobs
- Redis for caching
- ASP.NET Core Identity for authentication

### Frontend
- React 18 with TypeScript
- React Router for navigation
- React Query for state management
- SignalR JavaScript client
- CSS for styling

### Infrastructure
- PostgreSQL database
- Redis cache
- Docker containers

## Getting Started

### Prerequisites
- Docker and Docker Compose
- .NET 8.0 SDK (for local development)
- Node.js 18+ (for local development)

### Quick Start with Docker

1. Clone the repository
2. Start the infrastructure services:
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```
3. This will start PostgreSQL and Redis containers

### Local Development

#### Backend Setup
1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Update the database:
   ```bash
   dotnet ef database update
   ```
4. Run the API:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5001` or `http://localhost:5000`

#### Frontend Setup
1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the development server:
   ```bash
   npm start
   ```

The frontend will be available at `http://localhost:3000`

### Full Docker Deployment

To run the complete application with Docker:

```bash
docker-compose up --build
```

This will start:
- PostgreSQL database on port 5432
- Redis cache on port 6379
- ASP.NET Core API on port 5000
- React frontend on port 3000

## Configuration

### Database Connection
Update the connection string in `backend/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AirlineSimulationDb;Username=postgres;Password=postgres"
  }
}
```

### External APIs
Configure API keys in `backend/appsettings.json`:
```json
{
  "ExternalApis": {
    "AviationStack": {
      "ApiKey": "your-aviationstack-api-key"
    },
    "OpenWeatherMap": {
      "ApiKey": "your-openweathermap-api-key"
    }
  }
}
```

## Features

- **Real-time Flight Board**: Live flight information with SignalR updates
- **User Authentication**: Registration, login, and profile management
- **Flight Booking**: Complete booking flow with seat selection
- **Check-in Services**: Online check-in and boarding pass generation
- **Notifications**: Email and SMS notifications for flight updates
- **Baggage Tracking**: Track baggage status throughout journey
- **Premium Services**: Loyalty rewards and expedited security

## Development

### Entity Framework Migrations

To create a new migration:
```bash
cd backend
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Testing

Backend tests:
```bash
cd backend
dotnet test
```

Frontend tests:
```bash
cd frontend
npm test
```

## API Documentation

When running in development mode, Swagger documentation is available at:
`http://localhost:5000/swagger`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License.