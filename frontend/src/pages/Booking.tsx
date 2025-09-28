import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiService, CreateBookingRequest, PassengerRequest, SeatMapType } from '../services/api';
import './Booking.css';

interface FlightDetails {
  id: number;
  flightNumber: string;
  airline: string;
  originAirport: string;
  destinationAirport: string;
  scheduledDeparture: string;
  scheduledArrival: string;
  status: string;
  gate?: string;
  terminal?: string;
}

const Booking: React.FC = () => {
  const { flightNumber } = useParams<{ flightNumber: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [flight, setFlight] = useState<FlightDetails | null>(null);
  const [seatMap, setSeatMap] = useState<SeatMapType | null>(null);
  const [selectedSeats, setSelectedSeats] = useState<string[]>([]);
  const [passengers, setPassengers] = useState<PassengerRequest[]>([
    {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      dateOfBirth: '',
      seatClass: 'Economy'
    }
  ]);
  const [isLoading, setIsLoading] = useState(true);
  const [isBooking, setIsBooking] = useState(false);
  const [error, setError] = useState('');
  const [step, setStep] = useState(1); // 1: Passenger Info, 2: Seat Selection, 3: Payment, 4: Confirmation

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    
    loadFlightDetails();
  }, [flightNumber, user, navigate]);

  const loadFlightDetails = async () => {
    try {
      setIsLoading(true);
      const flightDate = new Date();
      const [flightDetails, seatMapData] = await Promise.all([
        apiService.getFlightDetails(flightNumber!, flightDate),
        apiService.getSeatMap(flightNumber!, flightDate)
      ]);
      
      setFlight(flightDetails.flight);
      setSeatMap(seatMapData);
    } catch (error) {
      console.error('Error loading flight details:', error);
      setError('Failed to load flight details');
    } finally {
      setIsLoading(false);
    }
  };

  const handlePassengerChange = (index: number, field: keyof PassengerRequest, value: string) => {
    const updatedPassengers = [...passengers];
    updatedPassengers[index] = { ...updatedPassengers[index], [field]: value };
    setPassengers(updatedPassengers);
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
      const updatedPassengers = passengers.filter((_, i) => i !== index);
      setPassengers(updatedPassengers);
      // Remove corresponding seat selection
      if (selectedSeats[index]) {
        setSelectedSeats(selectedSeats.filter((_, i) => i !== index));
      }
    }
  };

  const handleSeatSelection = (seatNumber: string) => {
    if (selectedSeats.includes(seatNumber)) {
      setSelectedSeats(selectedSeats.filter(seat => seat !== seatNumber));
    } else if (selectedSeats.length < passengers.length) {
      setSelectedSeats([...selectedSeats, seatNumber]);
    }
  };

  const calculateTotal = () => {
    const basePrice = 299.99;
    return passengers.reduce((total, passenger) => {
      const multiplier = passenger.seatClass === 'Economy' ? 1.0 :
                        passenger.seatClass === 'PremiumEconomy' ? 1.5 :
                        passenger.seatClass === 'Business' ? 2.5 : 4.0;
      return total + (basePrice * multiplier);
    }, 0);
  };

  const handleBooking = async () => {
    try {
      setIsBooking(true);
      setError('');

      const bookingRequest: CreateBookingRequest = {
        flightNumber: flightNumber!,
        flightDate: new Date().toISOString(),
        passengers,
        selectedSeats
      };

      const result = await apiService.createBooking(bookingRequest);
      
      // Navigate to confirmation page
      navigate(`/booking-confirmation/${result.confirmationNumber}`);
    } catch (error: any) {
      console.error('Booking failed:', error);
      setError(error.message || 'Booking failed. Please try again.');
    } finally {
      setIsBooking(false);
    }
  };

  const isStepValid = () => {
    switch (step) {
      case 1:
        return passengers.every(p => p.firstName && p.lastName);
      case 2:
        return selectedSeats.length === passengers.length;
      case 3:
        return true; // Payment step - always valid for demo
      default:
        return false;
    }
  };

  if (isLoading) {
    return (
      <div className="booking-container">
        <div className="loading">Loading flight details...</div>
      </div>
    );
  }

  if (!flight) {
    return (
      <div className="booking-container">
        <div className="error">Flight not found</div>
      </div>
    );
  }

  return (
    <div className="booking-container">
      <div className="booking-header">
        <h1>Book Flight {flight.flightNumber}</h1>
        <div className="flight-summary">
          <div className="route">
            <span className="airport">{flight.originAirport}</span>
            <span className="arrow">→</span>
            <span className="airport">{flight.destinationAirport}</span>
          </div>
          <div className="details">
            <span>{flight.airline}</span>
            <span>Departure: {new Date(flight.scheduledDeparture).toLocaleString()}</span>
            <span>Arrival: {new Date(flight.scheduledArrival).toLocaleString()}</span>
          </div>
        </div>
      </div>

      <div className="booking-steps">
        <div className={`step ${step >= 1 ? 'active' : ''}`}>
          <span className="step-number">1</span>
          <span className="step-title">Passenger Info</span>
        </div>
        <div className={`step ${step >= 2 ? 'active' : ''}`}>
          <span className="step-number">2</span>
          <span className="step-title">Seat Selection</span>
        </div>
        <div className={`step ${step >= 3 ? 'active' : ''}`}>
          <span className="step-number">3</span>
          <span className="step-title">Payment</span>
        </div>
        <div className={`step ${step >= 4 ? 'active' : ''}`}>
          <span className="step-number">4</span>
          <span className="step-title">Confirmation</span>
        </div>
      </div>

      <div className="booking-content">
        {step === 1 && (
          <div className="passenger-info">
            <h2>Passenger Information</h2>
            {passengers.map((passenger, index) => (
              <div key={index} className="passenger-form">
                <h3>Passenger {index + 1}</h3>
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
                    />
                  </div>
                  <div className="form-group">
                    <label>Seat Class</label>
                    <select
                      value={passenger.seatClass}
                      onChange={(e) => handlePassengerChange(index, 'seatClass', e.target.value)}
                    >
                      <option value="Economy">Economy</option>
                      <option value="PremiumEconomy">Premium Economy</option>
                      <option value="Business">Business</option>
                      <option value="First">First Class</option>
                    </select>
                  </div>
                </div>
                {passengers.length > 1 && (
                  <button
                    type="button"
                    className="btn btn-danger btn-sm"
                    onClick={() => removePassenger(index)}
                  >
                    Remove Passenger
                  </button>
                )}
              </div>
            ))}
            <button
              type="button"
              className="btn btn-secondary"
              onClick={addPassenger}
            >
              Add Another Passenger
            </button>
          </div>
        )}

        {step === 2 && (
          <div className="seat-selection">
            <h2>Select Seats</h2>
            <p>Select {passengers.length} seat{passengers.length > 1 ? 's' : ''} for your passengers</p>
            {seatMap && (
              <div className="seat-map">
                <div className="seat-legend">
                  <div className="legend-item">
                    <div className="seat available"></div>
                    <span>Available</span>
                  </div>
                  <div className="legend-item">
                    <div className="seat selected"></div>
                    <span>Selected</span>
                  </div>
                  <div className="legend-item">
                    <div className="seat occupied"></div>
                    <span>Occupied</span>
                  </div>
                </div>
                <div className="aircraft-layout">
                  {seatMap.rows.map((row) => (
                    <div key={row.rowNumber} className="seat-row">
                      <span className="row-number">{row.rowNumber}</span>
                      <div className="seats">
                        {row.seats.map((seat) => (
                          <button
                            key={seat.number}
                            className={`seat ${seat.isAvailable ? 'available' : 'occupied'} ${
                              selectedSeats.includes(seat.number) ? 'selected' : ''
                            }`}
                            onClick={() => seat.isAvailable && handleSeatSelection(seat.number)}
                            disabled={!seat.isAvailable}
                          >
                            {seat.number}
                          </button>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
            {selectedSeats.length > 0 && (
              <div className="selected-seats">
                <h3>Selected Seats:</h3>
                <div className="seat-list">
                  {selectedSeats.map((seat, index) => (
                    <span key={seat} className="selected-seat">
                      {seat} ({passengers[index]?.firstName} {passengers[index]?.lastName})
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {step === 3 && (
          <div className="payment-section">
            <h2>Payment Information</h2>
            <div className="payment-summary">
              <h3>Booking Summary</h3>
              <div className="summary-details">
                <div className="passenger-summary">
                  {passengers.map((passenger, index) => (
                    <div key={index} className="passenger-item">
                      <span>{passenger.firstName} {passenger.lastName}</span>
                      <span>{passenger.seatClass}</span>
                      <span>Seat: {selectedSeats[index] || 'TBD'}</span>
                      <span>${(299.99 * (passenger.seatClass === 'Economy' ? 1.0 :
                        passenger.seatClass === 'PremiumEconomy' ? 1.5 :
                        passenger.seatClass === 'Business' ? 2.5 : 4.0)).toFixed(2)}</span>
                    </div>
                  ))}
                </div>
                <div className="total">
                  <strong>Total: ${calculateTotal().toFixed(2)}</strong>
                </div>
              </div>
            </div>
            
            <div className="payment-form">
              <h3>Payment Details (Demo)</h3>
              <div className="demo-notice">
                <p>🔒 This is a demo booking system. No real payment will be processed.</p>
                <p>For demonstration purposes, we'll simulate a successful payment.</p>
              </div>
              <div className="form-group">
                <label>Card Number</label>
                <input type="text" value="4111 1111 1111 1111" disabled />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Expiry Date</label>
                  <input type="text" value="12/25" disabled />
                </div>
                <div className="form-group">
                  <label>CVV</label>
                  <input type="text" value="123" disabled />
                </div>
              </div>
              <div className="form-group">
                <label>Cardholder Name</label>
                <input type="text" value={`${user?.firstName} ${user?.lastName}`} disabled />
              </div>
            </div>
          </div>
        )}

        {error && <div className="error-message">{error}</div>}

        <div className="booking-navigation">
          {step > 1 && (
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setStep(step - 1)}
            >
              Previous
            </button>
          )}
          {step < 3 && (
            <button
              type="button"
              className="btn btn-primary"
              onClick={() => setStep(step + 1)}
              disabled={!isStepValid()}
            >
              Next
            </button>
          )}
          {step === 3 && (
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleBooking}
              disabled={isBooking || !isStepValid()}
            >
              {isBooking ? 'Processing...' : 'Complete Booking'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export default Booking;
