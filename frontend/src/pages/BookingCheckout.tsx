import React, { useState, useEffect } from 'react';
import { useParams, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiService, FlightSummary, WeatherResponse } from '../services/api';
import WeatherDisplay from '../components/WeatherDisplay';
import './Booking.css';

interface Passenger {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  seatClass: 'Economy' | 'Business' | 'First';
}

interface LocationState {
  flight: FlightSummary;
  flightDate: string;
}

const BookingCheckout: React.FC = () => {
  const { flightNumber } = useParams<{ flightNumber: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [passengers, setPassengers] = useState<Passenger[]>([{
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    dateOfBirth: '',
    seatClass: 'Economy'
  }]);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [weatherData, setWeatherData] = useState<WeatherResponse | null>(null);
  const [weatherLoading, setWeatherLoading] = useState(false);

  const state = location.state as LocationState;
  const flight = state?.flight;
  const flightDate = state?.flightDate || new Date().toISOString().split('T')[0];

  useEffect(() => {
    if (!flight || !user) {
      navigate('/flights');
      return;
    }

    // Load weather data for the flight
    loadWeatherData();
  }, [flight, user, navigate]);

  const loadWeatherData = async () => {
    if (!flight) return;

    try {
      setWeatherLoading(true);
      const weather = await apiService.getFlightWeather(flight.flightNumber, new Date(flightDate));
      setWeatherData(weather);
    } catch (error) {
      console.error('Failed to load weather data:', error);
    } finally {
      setWeatherLoading(false);
    }
  };

  const handlePassengerChange = (index: number, field: keyof Passenger, value: string) => {
    const updated = [...passengers];
    updated[index] = { ...updated[index], [field]: value };
    setPassengers(updated);
  };

  const addPassenger = () => {
    setPassengers([...passengers, {
      firstName: '',
      lastName: '',
      dateOfBirth: '',
      seatClass: 'Economy'
    }]);
  };

  const removePassenger = (index: number) => {
    if (passengers.length > 1) {
      setPassengers(passengers.filter((_, i) => i !== index));
    }
  };

  const calculatePrice = () => {
    const basePrices = {
      Economy: 299,
      Business: 899,
      First: 1599
    };

    return passengers.reduce((total, passenger) => {
      return total + basePrices[passenger.seatClass];
    }, 0);
  };

  const validatePassengers = () => {
    return passengers.every(p =>
      p.firstName.trim() &&
      p.lastName.trim() &&
      p.dateOfBirth &&
      p.seatClass
    );
  };

  const handleBooking = async () => {
    if (!validatePassengers()) {
      setError('Please fill in all passenger details');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const bookingRequest = {
        flightNumber: flight.flightNumber,
        flightDate: flightDate,
        passengers: passengers.map(p => ({
          firstName: p.firstName,
          lastName: p.lastName,
          dateOfBirth: p.dateOfBirth,
          seatClass: p.seatClass
        })),
        selectedSeats: [] // Will be handled by backend
      };

      const result = await apiService.createBooking(bookingRequest);

      // Navigate to confirmation page
      navigate('/booking-confirmation', {
        state: {
          booking: result,
          flight: flight,
          passengers: passengers,
          totalAmount: calculatePrice()
        }
      });

    } catch (err: any) {
      setError(err.message || 'Booking failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (!flight) {
    return <div className="loading">Loading flight details...</div>;
  }

  const formatDateTime = (dateTime: string) => {
    return new Date(dateTime).toLocaleString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <div className="booking-checkout">
      <div className="booking-header">
        <h1>✈️ Complete Your Booking</h1>
        <div className="flight-summary">
          <div className="flight-info">
            <h2>{flight.flightNumber} - {flight.airline}</h2>
            <div className="route">
              <span className="airport">{flight.originAirport}</span>
              <span className="arrow">✈️</span>
              <span className="airport">{flight.destinationAirport}</span>
            </div>
            <div className="times">
              <div>Departure: {formatDateTime(flight.scheduledDeparture)}</div>
              <div>Arrival: {formatDateTime(flight.scheduledArrival)}</div>
            </div>
            <div className="gate-terminal">
              Gate: {flight.gate || 'TBD'} | Terminal: {flight.terminal || 'TBD'}
            </div>
          </div>
        </div>
      </div>

      <div className="booking-content">
        <div className="passenger-section">
          <h3>Passenger Information</h3>
          {passengers.map((passenger, index) => (
            <div key={index} className="passenger-form">
              <div className="passenger-header">
                <h4>Passenger {index + 1}</h4>
                {passengers.length > 1 && (
                  <button
                    type="button"
                    className="btn btn-danger btn-sm"
                    onClick={() => removePassenger(index)}
                  >
                    Remove
                  </button>
                )}
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>First Name</label>
                  <input
                    type="text"
                    value={passenger.firstName}
                    onChange={(e) => handlePassengerChange(index, 'firstName', e.target.value)}
                    required
                  />
                </div>
                <div className="form-group">
                  <label>Last Name</label>
                  <input
                    type="text"
                    value={passenger.lastName}
                    onChange={(e) => handlePassengerChange(index, 'lastName', e.target.value)}
                    required
                  />
                </div>
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label>Date of Birth</label>
                  <input
                    type="date"
                    value={passenger.dateOfBirth}
                    onChange={(e) => handlePassengerChange(index, 'dateOfBirth', e.target.value)}
                    required
                  />
                </div>
                <div className="form-group">
                  <label>Seat Class</label>
                  <select
                    value={passenger.seatClass}
                    onChange={(e) => handlePassengerChange(index, 'seatClass', e.target.value as any)}
                  >
                    <option value="Economy">Economy - $299</option>
                    <option value="Business">Business - $899</option>
                    <option value="First">First Class - $1,599</option>
                  </select>
                </div>
              </div>
            </div>
          ))}

          <button
            type="button"
            className="btn btn-secondary"
            onClick={addPassenger}
          >
            + Add Another Passenger
          </button>
        </div>

        {/* Weather Information */}
        <div className="weather-section">
          <h3>🌤️ Weather Information</h3>
          <div className="weather-displays">
            <WeatherDisplay
              title="Departure Weather"
              location={`${flight.originAirport}`}
              weather={weatherData?.originWeather || null}
              isLoading={weatherLoading}
              error={weatherData === null && !weatherLoading ? 'Weather data unavailable' : undefined}
            />
            <WeatherDisplay
              title="Arrival Weather"
              location={`${flight.destinationAirport}`}
              weather={weatherData?.destinationWeather || null}
              isLoading={weatherLoading}
              error={weatherData === null && !weatherLoading ? 'Weather data unavailable' : undefined}
            />
          </div>
        </div>

        <div className="booking-summary">
          <h3>Booking Summary</h3>
          <div className="summary-details">
            <div className="summary-row">
              <span>Passengers:</span>
              <span>{passengers.length}</span>
            </div>
            {passengers.map((passenger, index) => (
              <div key={index} className="summary-row passenger-detail">
                <span>{passenger.firstName || 'Passenger'} {passenger.lastName} ({passenger.seatClass})</span>
                <span>${passenger.seatClass === 'Economy' ? 299 : passenger.seatClass === 'Business' ? 899 : 1599}</span>
              </div>
            ))}
            <div className="summary-row total">
              <span>Total Amount:</span>
              <span>${calculatePrice()}</span>
            </div>
          </div>

          {error && <div className="error-message">{error}</div>}

          <button
            className="btn btn-primary btn-large"
            onClick={handleBooking}
            disabled={loading || !validatePassengers()}
          >
            {loading ? 'Processing...' : `Complete Booking - $${calculatePrice()}`}
          </button>
        </div>
      </div>
    </div>
  );
};

export default BookingCheckout;