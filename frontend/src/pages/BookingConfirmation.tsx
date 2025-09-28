import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiService, BookingDetailsResponse } from '../services/api';
import './BookingConfirmation.css';

const BookingConfirmation: React.FC = () => {
  const { confirmationNumber } = useParams<{ confirmationNumber: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [booking, setBooking] = useState<BookingDetailsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [emailSent, setEmailSent] = useState(false);

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    
    if (confirmationNumber) {
      loadBookingDetails();
    }
  }, [confirmationNumber, user, navigate]);

  const loadBookingDetails = async () => {
    try {
      setIsLoading(true);
      const bookingDetails = await apiService.getBooking(confirmationNumber!);
      setBooking(bookingDetails);
      
      // Simulate sending email
      setTimeout(() => {
        setEmailSent(true);
      }, 2000);
    } catch (error) {
      console.error('Error loading booking details:', error);
      setError('Failed to load booking details');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCheckIn = async () => {
    try {
      await apiService.checkIn(confirmationNumber!);
      // Reload booking details to get updated status
      loadBookingDetails();
    } catch (error) {
      console.error('Check-in failed:', error);
      setError('Check-in failed. Please try again.');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (isLoading) {
    return (
      <div className="confirmation-container">
        <div className="loading">Loading booking details...</div>
      </div>
    );
  }

  if (error || !booking) {
    return (
      <div className="confirmation-container">
        <div className="error">
          {error || 'Booking not found'}
        </div>
      </div>
    );
  }

  return (
    <div className="confirmation-container">
      <div className="confirmation-header">
        <div className="success-icon">✅</div>
        <h1>Booking Confirmed!</h1>
        <p className="confirmation-number">Confirmation: {booking.confirmationNumber}</p>
      </div>

      <div className="confirmation-content">
        <div className="booking-status">
          <div className={`status-badge ${booking.status.toLowerCase()}`}>
            {booking.status}
          </div>
          <p>Booking created on {formatDate(booking.createdAt)}</p>
        </div>

        <div className="flight-details">
          <h2>Flight Details</h2>
          <div className="flight-card">
            <div className="flight-header">
              <div className="flight-number">{booking.flight.flightNumber}</div>
              <div className="airline">{booking.flight.airline}</div>
            </div>
            <div className="route-info">
              <div className="airport-section">
                <div className="airport-code">{booking.flight.originAirport}</div>
                <div className="airport-time">{formatTime(booking.flight.scheduledDeparture)}</div>
                <div className="airport-date">{new Date(booking.flight.scheduledDeparture).toLocaleDateString()}</div>
              </div>
              <div className="flight-arrow">→</div>
              <div className="airport-section">
                <div className="airport-code">{booking.flight.destinationAirport}</div>
                <div className="airport-time">{formatTime(booking.flight.scheduledArrival)}</div>
                <div className="airport-date">{new Date(booking.flight.scheduledArrival).toLocaleDateString()}</div>
              </div>
            </div>
            <div className="flight-info">
              <div className="info-item">
                <span className="label">Status:</span>
                <span className="value">{booking.flight.status}</span>
              </div>
              {booking.flight.gate && (
                <div className="info-item">
                  <span className="label">Gate:</span>
                  <span className="value">{booking.flight.gate}</span>
                </div>
              )}
              {booking.flight.terminal && (
                <div className="info-item">
                  <span className="label">Terminal:</span>
                  <span className="value">{booking.flight.terminal}</span>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="passenger-details">
          <h2>Passenger Information</h2>
          <div className="passengers-list">
            {booking.passengers.map((passenger, index) => (
              <div key={index} className="passenger-card">
                <div className="passenger-name">
                  {passenger.firstName} {passenger.lastName}
                </div>
                <div className="passenger-details">
                  <div className="detail-item">
                    <span className="label">Seat:</span>
                    <span className="value">{passenger.seatNumber || 'TBD'}</span>
                  </div>
                  <div className="detail-item">
                    <span className="label">Class:</span>
                    <span className="value">{passenger.seatClass}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="payment-summary">
          <h2>Payment Summary</h2>
          <div className="payment-card">
            <div className="payment-details">
              <div className="payment-item">
                <span className="label">Total Amount:</span>
                <span className="value amount">${booking.totalAmount.toFixed(2)}</span>
              </div>
              <div className="payment-item">
                <span className="label">Payment Status:</span>
                <span className="value status completed">Completed</span>
              </div>
              <div className="payment-item">
                <span className="label">Payment Method:</span>
                <span className="value">**** **** **** 1111</span>
              </div>
            </div>
          </div>
        </div>

        <div className="email-notification">
          <h2>Email Confirmation</h2>
          <div className="email-card">
            {emailSent ? (
              <div className="email-sent">
                <div className="email-icon">📧</div>
                <div className="email-content">
                  <h3>Email Sent Successfully!</h3>
                  <p>A confirmation email with your booking details and receipt has been sent to:</p>
                  <p className="email-address">{user?.email}</p>
                  <div className="email-details">
                    <p>The email includes:</p>
                    <ul>
                      <li>Flight details and itinerary</li>
                      <li>Passenger information</li>
                      <li>Payment receipt</li>
                      <li>Check-in instructions</li>
                      <li>Boarding pass (available 24 hours before departure)</li>
                    </ul>
                  </div>
                </div>
              </div>
            ) : (
              <div className="email-sending">
                <div className="loading-spinner"></div>
                <p>Sending confirmation email...</p>
              </div>
            )}
          </div>
        </div>

        <div className="next-steps">
          <h2>Next Steps</h2>
          <div className="steps-list">
            <div className="step-item">
              <div className="step-number">1</div>
              <div className="step-content">
                <h3>Check-in Online</h3>
                <p>Check-in opens 24 hours before departure</p>
                {booking.status === 'Confirmed' && (
                  <button 
                    className="btn btn-primary"
                    onClick={handleCheckIn}
                  >
                    Check-in Now
                  </button>
                )}
              </div>
            </div>
            <div className="step-item">
              <div className="step-number">2</div>
              <div className="step-content">
                <h3>Arrive at Airport</h3>
                <p>Arrive at least 2 hours before domestic flights, 3 hours for international</p>
              </div>
            </div>
            <div className="step-item">
              <div className="step-number">3</div>
              <div className="step-content">
                <h3>Boarding</h3>
                <p>Present your boarding pass and ID at the gate</p>
              </div>
            </div>
          </div>
        </div>

        {error && <div className="error-message">{error}</div>}

        <div className="confirmation-actions">
          <button 
            className="btn btn-secondary"
            onClick={() => navigate('/my-bookings')}
          >
            View All Bookings
          </button>
          <button 
            className="btn btn-primary"
            onClick={() => navigate('/')}
          >
            Back to Home
          </button>
        </div>
      </div>
    </div>
  );
};

export default BookingConfirmation;
