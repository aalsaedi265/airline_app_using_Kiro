import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { apiService, BookingDetailsResponse } from '../services/api';
import './MyBookings.css';

const MyBookings: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  
  const [bookings, setBookings] = useState<BookingDetailsResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<'all' | 'upcoming' | 'past'>('all');

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    
    loadBookings();
  }, [user, navigate]);

  const loadBookings = async () => {
    try {
      setIsLoading(true);
      // For demo purposes, we'll create some mock bookings
      // In a real app, this would call an API endpoint to get user's bookings
      const mockBookings: BookingDetailsResponse[] = [
        {
          confirmationNumber: 'ABC123',
          status: 'Confirmed',
          totalAmount: 599.98,
          createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
          flight: {
            id: 1,
            flightNumber: 'AA1234',
            airline: 'American Airlines',
            originAirport: 'ORD',
            destinationAirport: 'LAX',
            scheduledDeparture: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
            scheduledArrival: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000 + 4 * 60 * 60 * 1000).toISOString(),
            status: 'On Time',
            gate: 'A12',
            terminal: 'Terminal 1'
          },
          passengers: [
            {
              firstName: user?.firstName || 'John',
              lastName: user?.lastName || 'Doe',
              seatNumber: '12A',
              seatClass: 'Economy'
            }
          ]
        },
        {
          confirmationNumber: 'XYZ789',
          status: 'CheckedIn',
          totalAmount: 899.97,
          createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
          flight: {
            id: 2,
            flightNumber: 'UA5678',
            airline: 'United Airlines',
            originAirport: 'LAX',
            destinationAirport: 'JFK',
            scheduledDeparture: new Date(Date.now() + 1 * 24 * 60 * 60 * 1000).toISOString(),
            scheduledArrival: new Date(Date.now() + 1 * 24 * 60 * 60 * 1000 + 5 * 60 * 60 * 1000).toISOString(),
            status: 'On Time',
            gate: 'B8',
            terminal: 'Terminal 2'
          },
          passengers: [
            {
              firstName: user?.firstName || 'John',
              lastName: user?.lastName || 'Doe',
              seatNumber: '8C',
              seatClass: 'Business'
            }
          ]
        },
        {
          confirmationNumber: 'DEF456',
          status: 'Completed',
          totalAmount: 299.99,
          createdAt: new Date(Date.now() - 10 * 24 * 60 * 60 * 1000).toISOString(),
          flight: {
            id: 3,
            flightNumber: 'DL9012',
            airline: 'Delta Air Lines',
            originAirport: 'JFK',
            destinationAirport: 'MIA',
            scheduledDeparture: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
            scheduledArrival: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000 + 3 * 60 * 60 * 1000).toISOString(),
            status: 'Completed',
            gate: 'C15',
            terminal: 'Terminal 3'
          },
          passengers: [
            {
              firstName: user?.firstName || 'John',
              lastName: user?.lastName || 'Doe',
              seatNumber: '15F',
              seatClass: 'Economy'
            }
          ]
        }
      ];
      
      setBookings(mockBookings);
    } catch (error) {
      console.error('Error loading bookings:', error);
      setError('Failed to load bookings');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCheckIn = async (confirmationNumber: string) => {
    try {
      await apiService.checkIn(confirmationNumber);
      // Reload bookings to get updated status
      loadBookings();
    } catch (error) {
      console.error('Check-in failed:', error);
      setError('Check-in failed. Please try again.');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const formatTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return '#28a745';
      case 'checkedin':
        return '#007bff';
      case 'completed':
        return '#6c757d';
      case 'cancelled':
        return '#dc3545';
      default:
        return '#6c757d';
    }
  };

  const isUpcoming = (booking: BookingDetailsResponse) => {
    return new Date(booking.flight.scheduledDeparture) > new Date();
  };

  const filteredBookings = bookings.filter(booking => {
    switch (filter) {
      case 'upcoming':
        return isUpcoming(booking);
      case 'past':
        return !isUpcoming(booking);
      default:
        return true;
    }
  });

  if (isLoading) {
    return (
      <div className="bookings-container">
        <div className="loading">Loading your bookings...</div>
      </div>
    );
  }

  return (
    <div className="bookings-container">
      <div className="bookings-header">
        <h1>My Bookings</h1>
        <p>Manage your flight reservations and check-in online</p>
      </div>

      <div className="bookings-filters">
        <button
          className={`filter-btn ${filter === 'all' ? 'active' : ''}`}
          onClick={() => setFilter('all')}
        >
          All Bookings ({bookings.length})
        </button>
        <button
          className={`filter-btn ${filter === 'upcoming' ? 'active' : ''}`}
          onClick={() => setFilter('upcoming')}
        >
          Upcoming ({bookings.filter(isUpcoming).length})
        </button>
        <button
          className={`filter-btn ${filter === 'past' ? 'active' : ''}`}
          onClick={() => setFilter('past')}
        >
          Past ({bookings.filter(b => !isUpcoming(b)).length})
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="bookings-list">
        {filteredBookings.length === 0 ? (
          <div className="no-bookings">
            <div className="no-bookings-icon">✈️</div>
            <h3>No bookings found</h3>
            <p>
              {filter === 'upcoming' 
                ? "You don't have any upcoming flights."
                : filter === 'past'
                ? "You don't have any past flights."
                : "You haven't made any bookings yet."
              }
            </p>
            {filter === 'all' && (
              <button 
                className="btn btn-primary"
                onClick={() => navigate('/')}
              >
                Book a Flight
              </button>
            )}
          </div>
        ) : (
          filteredBookings.map((booking) => (
            <div key={booking.confirmationNumber} className="booking-card">
              <div className="booking-header">
                <div className="booking-info">
                  <h3>Confirmation: {booking.confirmationNumber}</h3>
                  <div className="booking-meta">
                    <span className="booking-date">
                      Booked on {formatDate(booking.createdAt)}
                    </span>
                    <span 
                      className="booking-status"
                      style={{ color: getStatusColor(booking.status) }}
                    >
                      {booking.status}
                    </span>
                  </div>
                </div>
                <div className="booking-amount">
                  ${booking.totalAmount.toFixed(2)}
                </div>
              </div>

              <div className="flight-info">
                <div className="flight-route">
                  <div className="airport-section">
                    <div className="airport-code">{booking.flight.originAirport}</div>
                    <div className="airport-time">{formatTime(booking.flight.scheduledDeparture)}</div>
                    <div className="airport-date">{formatDate(booking.flight.scheduledDeparture)}</div>
                  </div>
                  <div className="flight-arrow">→</div>
                  <div className="airport-section">
                    <div className="airport-code">{booking.flight.destinationAirport}</div>
                    <div className="airport-time">{formatTime(booking.flight.scheduledArrival)}</div>
                    <div className="airport-date">{formatDate(booking.flight.scheduledArrival)}</div>
                  </div>
                </div>
                <div className="flight-details">
                  <div className="flight-number">{booking.flight.flightNumber}</div>
                  <div className="airline">{booking.flight.airline}</div>
                  <div className="flight-status">{booking.flight.status}</div>
                  {booking.flight.gate && (
                    <div className="gate-info">Gate: {booking.flight.gate}</div>
                  )}
                </div>
              </div>

              <div className="passengers-info">
                <h4>Passengers</h4>
                <div className="passengers-list">
                  {booking.passengers.map((passenger, index) => (
                    <div key={index} className="passenger-item">
                      <span className="passenger-name">
                        {passenger.firstName} {passenger.lastName}
                      </span>
                      <span className="passenger-seat">
                        Seat: {passenger.seatNumber || 'TBD'}
                      </span>
                      <span className="passenger-class">
                        {passenger.seatClass}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="booking-actions">
                <button 
                  className="btn btn-secondary"
                  onClick={() => navigate(`/booking-confirmation/${booking.confirmationNumber}`)}
                >
                  View Details
                </button>
                {booking.status === 'Confirmed' && isUpcoming(booking) && (
                  <button 
                    className="btn btn-primary"
                    onClick={() => handleCheckIn(booking.confirmationNumber)}
                  >
                    Check-in
                  </button>
                )}
                {booking.status === 'CheckedIn' && (
                  <button 
                    className="btn btn-success"
                    onClick={() => navigate(`/booking-confirmation/${booking.confirmationNumber}`)}
                  >
                    View Boarding Pass
                  </button>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default MyBookings;
