using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AirlineSimulationApi.Data;
using AirlineSimulationApi.Services;
// using SendGrid.Extensions.DependencyInjection;
using AirlineSimulationApi.Hubs;
using DotNetEnv;

// Load .env file from project root (one level up from backend)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env file from: {envPath}");
}
else
{
    Console.WriteLine($"Warning: .env file not found at {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Configure logging to use console only
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add environment variable substitution
builder.Configuration.AddEnvironmentVariables();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration - build connection string from environment variables
var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? throw new InvalidOperationException("POSTGRES_DB environment variable is required.");
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? throw new InvalidOperationException("POSTGRES_USER environment variable is required.");
var dbPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? throw new InvalidOperationException("POSTGRES_PASSWORD environment variable is required.");
var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};";
Console.WriteLine($"Using database: {dbName} on {dbHost}:{dbPort} as user {dbUser}");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Database initialization will be done after app is built

// JWT Authentication - get from environment variable
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT_SECRET environment variable is required.");
}
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// In-memory caching (simpler than Redis)
builder.Services.AddMemoryCache();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// HTTP Clients for external APIs
builder.Services.AddHttpClient<IFlightDataService, AviationStackService>(client =>
{
    client.BaseAddress = new Uri("https://api.aviationstack.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IWeatherService, OpenWeatherMapService>(client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Application services (simplified)
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBaggageService, BaggageService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Email configuration (using simulation for demo purposes)
Console.WriteLine("Using simulated email service for demo purposes.");

// Background services
builder.Services.AddHostedService<FlightUpdateBackgroundService>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Run migrations (this will create database and tables if they don't exist)
        context.Database.Migrate();

        // Seed some sample data if database is empty
        if (!context.Flights.Any())
        {
            SeedSampleData(context);
        }

        Console.WriteLine("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
        // For development, you might want to drop and recreate
        try
        {
            Console.WriteLine("Attempting to recreate database...");
            context.Database.EnsureDeleted();
            context.Database.Migrate();
            SeedSampleData(context);
            Console.WriteLine("Database recreated successfully");
        }
        catch (Exception recreateEx)
        {
            Console.WriteLine($"Failed to recreate database: {recreateEx.Message}");
            throw;
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<FlightUpdatesHub>("/flightUpdatesHub");

app.Run();

static void SeedSampleData(ApplicationDbContext context)
{
    var tomorrow = DateTime.UtcNow.Date.AddDays(1);
    var flights = new[]
    {
        new AirlineSimulationApi.Models.Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = tomorrow.AddHours(8),
            ScheduledArrival = tomorrow.AddHours(10),
            Status = AirlineSimulationApi.Models.FlightStatus.OnTime,
            Gate = "A12",
            Terminal = "1"
        },
        new AirlineSimulationApi.Models.Flight
        {
            FlightNumber = "UA456",
            Airline = "United Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "JFK",
            ScheduledDeparture = tomorrow.AddHours(10),
            ScheduledArrival = tomorrow.AddHours(13),
            Status = AirlineSimulationApi.Models.FlightStatus.OnTime,
            Gate = "B8",
            Terminal = "2"
        },
        new AirlineSimulationApi.Models.Flight
        {
            FlightNumber = "DL789",
            Airline = "Delta Air Lines",
            OriginAirport = "ORD",
            DestinationAirport = "ATL",
            ScheduledDeparture = tomorrow.AddHours(14),
            ScheduledArrival = tomorrow.AddHours(16),
            Status = AirlineSimulationApi.Models.FlightStatus.Boarding,
            Gate = "C15",
            Terminal = "1"
        }
    };

    context.Flights.AddRange(flights);
    context.SaveChanges();
    Console.WriteLine("Sample flight data seeded");
}