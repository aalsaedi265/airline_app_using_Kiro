using AirlineSimulationApi.Models;

namespace AirlineSimulationApi.Services;

public interface IEmailService
{
    Task<bool> SendBookingConfirmationAsync(Booking booking, string userEmail);
    Task<bool> SendCheckInConfirmationAsync(Booking booking, string userEmail);
    Task<bool> SendFlightUpdateAsync(Booking booking, string userEmail, string updateMessage);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendBookingConfirmationAsync(Booking booking, string userEmail)
    {
        try
        {
            _logger.LogInformation("Sending booking confirmation email to {Email} for booking {ConfirmationNumber}",
                userEmail, booking.ConfirmationNumber);

            // Simulate email sending delay
            await Task.Delay(1000);

            var emailContent = GenerateBookingConfirmationText(booking);

            _logger.LogInformation("Booking confirmation email sent successfully to {Email}", userEmail);
            _logger.LogInformation("Email content preview: Flight {FlightNumber} - Confirmation {ConfirmationNumber}",
                booking.Flight.FlightNumber, booking.ConfirmationNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmation email to {Email}", userEmail);
            return false;
        }
    }

    public async Task<bool> SendCheckInConfirmationAsync(Booking booking, string userEmail)
    {
        try
        {
            _logger.LogInformation("Sending check-in confirmation email to {Email} for booking {ConfirmationNumber}",
                userEmail, booking.ConfirmationNumber);

            // Simulate email sending delay
            await Task.Delay(500);

            _logger.LogInformation("Check-in confirmation email sent successfully to {Email}", userEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send check-in confirmation email to {Email}", userEmail);
            return false;
        }
    }

    public async Task<bool> SendFlightUpdateAsync(Booking booking, string userEmail, string updateMessage)
    {
        try
        {
            _logger.LogInformation("Sending flight update email to {Email} for booking {ConfirmationNumber}",
                userEmail, booking.ConfirmationNumber);

            // Simulate email sending delay
            await Task.Delay(500);

            _logger.LogInformation("Flight update email sent successfully to {Email}", userEmail);
            _logger.LogInformation("Update message: {UpdateMessage}", updateMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send flight update email to {Email}", userEmail);
            return false;
        }
    }

    private string GenerateBookingConfirmationEmail(Booking booking)
    {
        var flight = booking.Flight;
        var passengers = booking.Passengers;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Flight Booking Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .flight-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .passenger-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .payment-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 0.9rem; }}
        .highlight {{ color: #667eea; font-weight: bold; }}
        .success {{ color: #28a745; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✈️ Flight Booking Confirmed!</h1>
            <p>Confirmation Number: <span class='highlight'>{booking.ConfirmationNumber}</span></p>
        </div>
        
        <div class='content'>
            <h2>Booking Details</h2>
            <div class='flight-info'>
                <h3>Flight Information</h3>
                <p><strong>Flight:</strong> {flight.FlightNumber} - {flight.Airline}</p>
                <p><strong>Route:</strong> {flight.OriginAirport} → {flight.DestinationAirport}</p>
                <p><strong>Departure:</strong> {flight.ScheduledDeparture:MMMM dd, yyyy 'at' h:mm tt}</p>
                <p><strong>Arrival:</strong> {flight.ScheduledArrival:MMMM dd, yyyy 'at' h:mm tt}</p>
                <p><strong>Status:</strong> {flight.Status}</p>
                {(flight.Gate != null ? $"<p><strong>Gate:</strong> {flight.Gate}</p>" : "")}
                {(flight.Terminal != null ? $"<p><strong>Terminal:</strong> {flight.Terminal}</p>" : "")}
            </div>
            
            <div class='passenger-info'>
                <h3>Passenger Information</h3>
                {string.Join("", passengers.Select(p => $@"
                <div style='margin-bottom: 15px; padding: 10px; background: #f8f9fa; border-radius: 4px;'>
                    <p><strong>Name:</strong> {p.FirstName} {p.LastName}</p>
                    <p><strong>Seat:</strong> {p.SeatNumber ?? "TBD"}</p>
                    <p><strong>Class:</strong> {p.SeatClass}</p>
                </div>"))}
            </div>
            
            <div class='payment-info'>
                <h3>Payment Information</h3>
                <p><strong>Total Amount:</strong> <span class='success'>${booking.TotalAmount:F2}</span></p>
                <p><strong>Payment Status:</strong> <span class='success'>Completed</span></p>
                <p><strong>Booking Date:</strong> {booking.CreatedAt:MMMM dd, yyyy 'at' h:mm tt}</p>
            </div>
            
            <h3>Next Steps</h3>
            <ul>
                <li>Check-in online 24 hours before departure</li>
                <li>Arrive at the airport at least 2 hours before domestic flights</li>
                <li>Bring a valid photo ID and your confirmation number</li>
                <li>Check for any flight updates before traveling</li>
            </ul>
            
            <div class='footer'>
                <p>Thank you for choosing our airline service!</p>
                <p>For assistance, contact us at support@airline.com or call 1-800-FLY-HELP</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateCheckInConfirmationEmail(Booking booking)
    {
        var flight = booking.Flight;
        var passengers = booking.Passengers;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Check-in Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .boarding-pass {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border: 2px solid #28a745; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 0.9rem; }}
        .highlight {{ color: #28a745; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Check-in Complete!</h1>
            <p>You're all set for your flight</p>
        </div>
        
        <div class='content'>
            <div class='boarding-pass'>
                <h3>Digital Boarding Pass</h3>
                <p><strong>Flight:</strong> {flight.FlightNumber}</p>
                <p><strong>Passenger:</strong> {passengers.First().FirstName} {passengers.First().LastName}</p>
                <p><strong>Seat:</strong> {passengers.First().SeatNumber ?? "TBD"}</p>
                <p><strong>Gate:</strong> {flight.Gate ?? "TBD"}</p>
                <p><strong>Boarding Time:</strong> {flight.ScheduledDeparture.AddMinutes(-30):h:mm tt}</p>
            </div>
            
            <h3>Important Reminders</h3>
            <ul>
                <li>Arrive at the gate 30 minutes before departure</li>
                <li>Have your boarding pass and ID ready</li>
                <li>Check gate information as it may change</li>
            </ul>
            
            <div class='footer'>
                <p>Safe travels!</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateFlightUpdateEmail(Booking booking, string updateMessage)
    {
        var flight = booking.Flight;
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Flight Update</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ffc107 0%, #fd7e14 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .update-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 0.9rem; }}
        .highlight {{ color: #ffc107; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📢 Flight Update</h1>
            <p>Important information about your flight</p>
        </div>

        <div class='content'>
            <div class='update-info'>
                <h3>Flight: {flight.FlightNumber}</h3>
                <p><strong>Route:</strong> {flight.OriginAirport} → {flight.DestinationAirport}</p>
                <p><strong>Date:</strong> {flight.ScheduledDeparture:MMMM dd, yyyy}</p>
                <p><strong>Update:</strong> {updateMessage}</p>
            </div>

            <div class='footer'>
                <p>We apologize for any inconvenience.</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateBookingConfirmationText(Booking booking)
    {
        var flight = booking.Flight;
        var passengers = booking.Passengers;

        return $@"
Flight Booking Confirmation - {booking.ConfirmationNumber}

Dear Customer,

Your flight booking has been confirmed!

BOOKING DETAILS:
Confirmation Number: {booking.ConfirmationNumber}

FLIGHT INFORMATION:
Flight: {flight.FlightNumber} - {flight.Airline}
Route: {flight.OriginAirport} → {flight.DestinationAirport}
Departure: {flight.ScheduledDeparture:MMMM dd, yyyy 'at' h:mm tt}
Arrival: {flight.ScheduledArrival:MMMM dd, yyyy 'at' h:mm tt}
Status: {flight.Status}
{(flight.Gate != null ? $"Gate: {flight.Gate}" : "")}
{(flight.Terminal != null ? $"Terminal: {flight.Terminal}" : "")}

PASSENGER INFORMATION:
{string.Join("\n", passengers.Select(p => $"- {p.FirstName} {p.LastName} ({p.SeatClass}) - Seat: {p.SeatNumber ?? "TBD"}"))}

PAYMENT INFORMATION:
Total Amount: ${booking.TotalAmount:F2}
Payment Status: Completed
Booking Date: {booking.CreatedAt:MMMM dd, yyyy 'at' h:mm tt}

NEXT STEPS:
- Check-in online 24 hours before departure
- Arrive at the airport at least 2 hours before domestic flights
- Bring a valid photo ID and your confirmation number
- Check for any flight updates before traveling

Thank you for choosing our airline service!

For assistance, contact us at support@airline.com or call 1-800-FLY-HELP
";
    }

    private string GenerateCheckInConfirmationText(Booking booking)
    {
        var flight = booking.Flight;
        var passengers = booking.Passengers;

        return $@"
Check-in Complete!

Dear Customer,

You're all set for your flight!

DIGITAL BOARDING PASS:
Flight: {flight.FlightNumber}
Passenger: {passengers.First().FirstName} {passengers.First().LastName}
Seat: {passengers.First().SeatNumber ?? "TBD"}
Gate: {flight.Gate ?? "TBD"}
Boarding Time: {flight.ScheduledDeparture.AddMinutes(-30):h:mm tt}

IMPORTANT REMINDERS:
- Arrive at the gate 30 minutes before departure
- Have your boarding pass and ID ready
- Check gate information as it may change

Safe travels!
";
    }
}
