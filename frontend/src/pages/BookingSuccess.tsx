import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { FlightSummary, BookingResponse } from '../services/api';
import './BookingConfirmation.css';

interface Passenger {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  seatClass: string;
}

interface LocationState {
  booking: BookingResponse;
  flight: FlightSummary;
  passengers: Passenger[];
  totalAmount: number;
}

const BookingSuccess: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [emailSent, setEmailSent] = useState(false);

  const state = location.state as LocationState;

  useEffect(() => {
    if (!state || !user) {
      navigate('/flights');
      return;
    }

    // Simulate email sending
    const sendConfirmationEmail = async () => {
      try {
        // In a real app, you'd call an API endpoint here
        await new Promise(resolve => setTimeout(resolve, 2000));
        setEmailSent(true);
      } catch (error) {
        console.error('Failed to send confirmation email:', error);
      }
    };

    sendConfirmationEmail();
  }, [state, user, navigate]);

  if (!state) {
    return <div className="loading">Loading...</div>;
  }

  const { booking, flight, passengers, totalAmount } = state;

  const formatDateTime = (dateTime: string) => {
    return new Date(dateTime).toLocaleString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatDate = (dateTime: string) => {
    return new Date(dateTime).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  const printReceipt = () => {
    window.print();
  };

  return (
    <div className="booking-success">
      <div className="success-header">
        <div className="success-icon">✅</div>
        <h1>Booking Confirmed!</h1>
        <p>Your flight has been successfully booked.</p>
        <div className="confirmation-number">
          <strong>Confirmation Number: {booking.confirmationNumber}</strong>
        </div>
      </div>

      <div className="booking-receipt">
        <div className="receipt-header">
          <h2>🏙️ Ahmed's Chicago Airport Project ✈️</h2>
          <p>Booking Receipt</p>
        </div>

        <div className="receipt-content">
          <div className="section">
            <h3>Flight Information</h3>
            <div className="flight-details">
              <div className="detail-row">
                <span className="label">Flight:</span>
                <span className="value">{flight.flightNumber} - {flight.airline}</span>
              </div>
              <div className="detail-row">
                <span className="label">Route:</span>
                <span className="value">{flight.originAirport} ✈️ {flight.destinationAirport}</span>
              </div>
              <div className="detail-row">
                <span className="label">Date:</span>
                <span className="value">{formatDate(flight.scheduledDeparture)}</span>
              </div>
              <div className="detail-row">
                <span className="label">Departure:</span>
                <span className="value">{formatDateTime(flight.scheduledDeparture)}</span>
              </div>
              <div className="detail-row">
                <span className="label">Arrival:</span>
                <span className="value">{formatDateTime(flight.scheduledArrival)}</span>
              </div>
              <div className="detail-row">
                <span className="label">Gate:</span>
                <span className="value">{flight.gate || 'Will be announced'}</span>
              </div>
              <div className="detail-row">
                <span className="label">Terminal:</span>
                <span className="value">{flight.terminal || 'Will be announced'}</span>
              </div>
            </div>
          </div>

          <div className="section">
            <h3>Passenger Information</h3>
            <div className="passengers-list">
              {passengers.map((passenger, index) => (
                <div key={index} className="passenger-item">
                  <div className="passenger-name">
                    👤 {passenger.firstName} {passenger.lastName}
                  </div>
                  <div className="passenger-details">
                    <span>Class: {passenger.seatClass}</span>
                    <span>DOB: {new Date(passenger.dateOfBirth).toLocaleDateString()}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="section">
            <h3>Booking Details</h3>
            <div className="booking-details">
              <div className="detail-row">
                <span className="label">Confirmation Number:</span>
                <span className="value">{booking.confirmationNumber}</span>
              </div>
              <div className="detail-row">
                <span className="label">Booking Date:</span>
                <span className="value">{formatDateTime(booking.createdAt)}</span>
              </div>
              <div className="detail-row">
                <span className="label">Status:</span>
                <span className="value">{booking.status}</span>
              </div>
              <div className="detail-row">
                <span className="label">Passengers:</span>
                <span className="value">{passengers.length}</span>
              </div>
            </div>
          </div>

          <div className="section">
            <h3>Payment Summary</h3>
            <div className="payment-summary">
              {passengers.map((passenger, index) => (
                <div key={index} className="payment-row">
                  <span>{passenger.firstName} {passenger.lastName} ({passenger.seatClass})</span>
                  <span>${passenger.seatClass === 'Economy' ? '299.00' : passenger.seatClass === 'Business' ? '899.00' : '1,599.00'}</span>
                </div>
              ))}
              <div className="payment-row total">
                <span><strong>Total Paid:</strong></span>
                <span><strong>${totalAmount.toFixed(2)}</strong></span>
              </div>
            </div>
          </div>

          <div className="section">
            <h3>Important Information</h3>
            <div className="important-info">
              <p>✈️ Please arrive at the airport at least 2 hours before domestic flights</p>
              <p>🆔 Valid photo ID required for all passengers</p>
              <p>🧳 Check baggage allowance and restrictions</p>
              <p>📱 Check-in online 24 hours before departure</p>
              <p>🔄 Changes and cancellations subject to airline policy</p>
            </div>
          </div>
        </div>

        <div className="email-status">
          {emailSent ? (
            <div className="email-success">
              ✅ Confirmation email sent to {user?.email}
            </div>
          ) : (
            <div className="email-sending">
              ⏳ Sending confirmation email to {user?.email}...
            </div>
          )}
        </div>

        <div className="receipt-actions">
          <button className="btn btn-primary" onClick={printReceipt}>
            🖨️ Print Receipt
          </button>
          <Link to="/flights" className="btn btn-secondary">
            ✈️ Book Another Flight
          </Link>
          <Link to="/my-bookings" className="btn btn-secondary">
            📋 View My Bookings
          </Link>
        </div>
      </div>
    </div>
  );
};

export default BookingSuccess;