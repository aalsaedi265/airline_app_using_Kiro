# Airline Simulation Application

A comprehensive airline simulation web application built with ASP.NET Core and React TypeScript, featuring real-time flight tracking, booking management, and airline services. This project demonstrates modern full-stack development with a focus on simplicity and maintainability.

## 🎯 Project Vision

Create an interactive, feature-rich web application that simulates the core services of a modern airline, providing a seamless user experience for flight tracking, booking, and management, all powered by real-time flight and weather data.

## 🏗️ Architecture Strategy

### Why This Technology Stack?

**Backend: ASP.NET Core + PostgreSQL**
- **C#**: Familiar language, strong typing, excellent tooling
- **PostgreSQL**: Reliable, open-source database you know well
- **Entity Framework Core**: Simplifies database operations with LINQ
- **SignalR**: Built-in real-time communication (no complex WebSocket setup)
- **IMemoryCache**: Simple in-memory caching (no Redis complexity)
- **BackgroundService**: Built-in background jobs (no Hangfire complexity)

**Frontend: React + TypeScript**
- **React**: Industry standard, component-based architecture
- **TypeScript**: Type safety, better development experience
- **React Query**: Handles API state management and caching
- **CSS Modules**: Simple, scoped styling

**Real API Strategy**
- **Why Real APIs?**: Authentic aviation data and weather conditions
- **Live Data**: Real-time flight schedules, delays, and weather updates
- **Professional Experience**: Industry-standard data sources
- **Learning Value**: Understanding external API integration

### 🔄 Technology Decisions & Alternatives

**Why We Chose These Technologies Over Alternatives:**

**Caching: IMemoryCache vs Redis**
- ✅ **Chosen**: IMemoryCache - Built into ASP.NET Core, no external dependencies
- ❌ **Alternative**: Redis - More powerful but adds complexity, requires separate service
- **Reason**: For learning and demos, in-memory caching is sufficient and simpler

**Background Jobs: BackgroundService vs Hangfire**
- ✅ **Chosen**: BackgroundService - Built into ASP.NET Core, no database dependencies
- ❌ **Alternative**: Hangfire - More features but requires database setup and configuration
- **Reason**: Our background jobs are simple (flight updates), built-in service is adequate

**Data Sources: Real APIs vs Mock Data**
- ✅ **Chosen**: AviationStack/OpenWeatherMap APIs - Real flight and weather data
- ❌ **Alternative**: Mock Data Services - No external dependencies but not realistic
- **Reason**: Real APIs provide authentic aviation data and weather conditions

**Authentication: JWT vs ASP.NET Core Identity**
- ✅ **Chosen**: JWT - Simple, stateless, easy to understand
- ❌ **Alternative**: ASP.NET Core Identity - Full-featured but more complex
- **Reason**: JWT is sufficient for our needs and easier to implement

**Database: PostgreSQL vs SQL Server**
- ✅ **Chosen**: PostgreSQL - Open source, cross-platform, you're familiar with it
- ❌ **Alternative**: SQL Server - Microsoft's database, more .NET integration
- **Reason**: PostgreSQL is free, open-source, and you already know it

**Frontend State: React Query vs Redux**
- ✅ **Chosen**: React Query - Perfect for server state management
- ❌ **Alternative**: Redux - More complex, better for complex client state
- **Reason**: React Query handles API state beautifully, Redux would be overkill

> 📋 **Detailed Architecture Decisions**: See [ARCHITECTURE_DECISIONS.md](./ARCHITECTURE_DECISIONS.md) for comprehensive explanations of each technology choice, including alternatives considered and consequences of each decision.

## 📁 Project Structure

```
airline-simulation-app/
├── backend/                 # ASP.NET Core Web API
│   ├── Controllers/         # API Controllers (Flights, Bookings, Auth)
│   ├── Services/           # Business Logic (Flight, Booking, Weather, Payment, Baggage)
│   ├── Models/             # Data Models (Flight, Booking, User)
│   ├── Data/               # Entity Framework DbContext
│   └── Hubs/               # SignalR Hubs (Real-time updates)
├── frontend/               # React TypeScript SPA
│   ├── src/
│   │   ├── components/     # Reusable React Components
│   │   ├── pages/          # Page Components (FlightBoard, Login, etc.)
│   │   ├── contexts/       # React Contexts (Auth, SignalR)
│   │   └── services/       # API Service Layer
└── docker-compose.yml      # Docker configuration
```

## 🛠️ Technology Stack

### Backend
- **ASP.NET Core 8.0** - High-performance web API framework
- **PostgreSQL** - Reliable, open-source database
- **Entity Framework Core** - ORM for database operations
- **SignalR** - Real-time communication (WebSockets)
- **IMemoryCache** - In-memory caching (simpler than Redis)
- **BackgroundService** - Background job processing
- **JWT Authentication** - Simple, stateless authentication

### Frontend
- **React 18** - Component-based UI library
- **TypeScript** - Type-safe JavaScript
- **React Router** - Client-side routing
- **React Query** - Server state management and caching
- **SignalR Client** - Real-time updates from backend
- **CSS Modules** - Scoped, maintainable styling

### Infrastructure
- **Docker** - Containerization for consistent deployment
- **Docker Compose** - Multi-container orchestration
- **PostgreSQL** - Primary database
- **Mock Data Services** - Realistic flight and weather data

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK (required for local development)
- Node.js 18+ (required for frontend development)
- Docker and Docker Compose (optional, for containerized database)

### Environment Setup (Required First)

1. **Create Environment File**
   Create a `.env` file in the **root directory** with the following configuration:
   ```bash
   # Database Configuration
   POSTGRES_PASSWORD=your_secure_password_here
   POSTGRES_USER=postgres
   POSTGRES_DB=AirlineSimulationDb

   # Redis Configuration
   REDIS_CONNECTION_STRING=localhost:6379
   REDIS_PASSWORD=

   # JWT Configuration (Must be at least 32 characters)
   JWT_SECRET=your-super-secure-jwt-secret-key-that-is-at-least-32-characters-long

   # API Configuration
   API_PORT=5001
   FRONTEND_PORT=3000

   # External APIs
   AVIATION_STACK_API_KEY=your_aviation_stack_api_key_here
   OPENWEATHER_API_KEY=your_openweather_api_key_here
   ```

### Quick Start (Recommended)

#### 1. Database Setup
Start PostgreSQL database using Docker:
```bash
docker-compose -f docker-compose.dev.yml up -d
```

#### 2. Backend Setup
```bash
cd backend
dotnet restore
dotnet build
dotnet ef database drop --force  # Clean start
dotnet ef database update       # Apply migrations
dotnet run --urls http://localhost:5001
```

#### 3. Frontend Setup (New Terminal)
```bash
cd frontend
npm install
npm start
```

### Application Access
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5001
- **API Documentation**: http://localhost:5001/swagger

### Important Notes
- **Backend runs on port 5001** (not 5000) to avoid conflicts
- **Environment variables** are loaded from root `.env` file
- **Database migrations** must be applied before first run
- **JWT secret** must be at least 32 characters for security

### Full Docker Deployment

To run the complete application with Docker:

```bash
docker-compose up --build
```

This will start:
- PostgreSQL database on port 5432
- ASP.NET Core API on port 5000
- React frontend on port 3000

## ✨ Features

### ✅ **Implemented Core Features**

**Real-time Flight Board**
- Live flight departures and arrivals for Chicago O'Hare (ORD)
- Essential flight data: Airline, Flight #, Destination/Origin, Scheduled/Estimated Times, Status, Gate/Terminal
- Search and filtering capabilities
- Real-time updates via SignalR

**Weather Integration**
- Current weather conditions for origin and destination cities
- Mock weather data with realistic conditions
- Integrated into flight details view

**User Authentication**
- JWT-based authentication system
- User registration and login
- Secure session management

**Booking System**
- Complete booking flow with seat selection
- Visual seat map with interactive selection
- Seat class selection (First, Business, Premium Economy, Economy)
- Simulated payment gateway integration
- Booking confirmation and management

**Check-in Services**
- Online check-in portal with confirmation number lookup
- Boarding pass generation with QR codes
- 24-hour check-in availability window

**Notification System**
- Real-time flight updates via SignalR
- Email and SMS notification simulation
- Flight status change notifications

**Baggage Tracking**
- Baggage tracking number generation
- Real-time status updates (Checked, In Transit, Loaded, Arrived, Delivered)
- Status history and location tracking

**Background Services**
- Automated flight status updates
- Mock data generation for realistic experience
- Background job processing

### 🎯 **Key Benefits**

- **No External Dependencies**: Uses mock data, no API keys required
- **Always Works**: No external service downtime
- **Realistic Experience**: Generated data mimics real airline operations
- **Easy to Demo**: Consistent, predictable data for presentations
- **Simple Architecture**: Clean, maintainable codebase
- **Full-Stack Integration**: Complete React + ASP.NET Core application

## Configuration

All configuration is now handled through the root `.env` file. The application automatically loads environment variables and builds connection strings programmatically.

### Key Configuration Points:
- **Database**: PostgreSQL connection built from environment variables
- **JWT**: Secret key loaded from JWT_SECRET environment variable
- **APIs**: External API keys loaded from environment variables
- **Ports**: Backend runs on port 5001, Frontend on port 3000

### Troubleshooting Common Issues:

1. **Database Connection Errors**: Ensure PostgreSQL is running and environment variables are correct
2. **JWT Token Issues**: Verify JWT_SECRET is at least 32 characters long
3. **Port Conflicts**: Backend uses port 5001 (not 5000) to avoid conflicts
4. **Migration Errors**: Use `dotnet ef database drop --force` for clean database reset

## 🎮 Demo Flow

1. **Register** a new account
2. **Login** to access the system
3. **View Flight Board** - See live flight data with real-time updates
4. **Book a Flight** - Select seats and complete payment
5. **Check-in** - Use confirmation number to check in
6. **Track Baggage** - Monitor baggage status
7. **Receive Notifications** - Get real-time flight updates

## 🛠️ Development

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

### API Documentation

When running in development mode, Swagger documentation is available at:
`http://localhost:5000/swagger`

### Project Structure Details

**Backend Services:**
- `FlightService` - Manages flight data and real-time updates
- `BookingService` - Handles booking creation and management
- `PaymentService` - Processes payments (simulated)
- `NotificationService` - Sends real-time notifications
- `BaggageService` - Manages baggage tracking
- `MockFlightDataService` - Generates realistic flight data
- `MockWeatherService` - Generates weather data

**Frontend Components:**
- `FlightBoard` - Main flight display with search/filtering
- `SeatMap` - Interactive seat selection component
- `AuthContext` - Authentication state management
- `SignalRContext` - Real-time connection management
- `apiService` - Centralized API communication

### Key Design Patterns

- **Repository Pattern**: Entity Framework handles data access
- **Service Layer**: Business logic separated from controllers
- **Dependency Injection**: All services registered in Program.cs
- **Background Services**: Automated flight status updates
- **Real-time Communication**: SignalR for live updates
- **Mock Data Strategy**: No external dependencies

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.