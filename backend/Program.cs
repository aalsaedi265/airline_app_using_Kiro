using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AirlineSimulationApi.Data;
using AirlineSimulationApi.Services;
using AirlineSimulationApi.Hubs;
using DotNetEnv;

// Load environment variables from .env file
Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration - substitute environment variables
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=AirlineSimulationDb;Username=postgres;Password=postgres";

var connectionString = rawConnectionString
    .Replace("${POSTGRES_DB}", Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "AirlineSimulationDb")
    .Replace("${POSTGRES_USER}", Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres")
    .Replace("${POSTGRES_PASSWORD}", Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Ensure database is created and migrated
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Create database if it doesn't exist
        context.Database.EnsureCreated();
        
        // Run migrations
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
    }
}

// JWT Authentication (simplified) - substitute environment variables
var rawJwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
var jwtSecret = rawJwtSecret
    .Replace("${JWT_SECRET}", Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-key-that-is-at-least-32-characters-long");
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
        policy.WithOrigins("http://localhost:3000")
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

// Background services
builder.Services.AddHostedService<FlightUpdateBackgroundService>();

var app = builder.Build();

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
    var flights = new[]
    {
        new AirlineSimulationApi.Models.Flight
        {
            FlightNumber = "AA123",
            Airline = "American Airlines",
            OriginAirport = "ORD",
            DestinationAirport = "LAX",
            ScheduledDeparture = DateTime.UtcNow.Date.AddHours(8),
            ScheduledArrival = DateTime.UtcNow.Date.AddHours(10),
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
            ScheduledDeparture = DateTime.UtcNow.Date.AddHours(10),
            ScheduledArrival = DateTime.UtcNow.Date.AddHours(13),
            Status = AirlineSimulationApi.Models.FlightStatus.Delayed,
            Gate = "B8",
            Terminal = "2"
        },
        new AirlineSimulationApi.Models.Flight
        {
            FlightNumber = "DL789",
            Airline = "Delta Air Lines",
            OriginAirport = "ORD",
            DestinationAirport = "ATL",
            ScheduledDeparture = DateTime.UtcNow.Date.AddHours(14),
            ScheduledArrival = DateTime.UtcNow.Date.AddHours(16),
            Status = AirlineSimulationApi.Models.FlightStatus.Boarding,
            Gate = "C15",
            Terminal = "1"
        }
    };

    context.Flights.AddRange(flights);
    context.SaveChanges();
    Console.WriteLine("Sample flight data seeded");
}