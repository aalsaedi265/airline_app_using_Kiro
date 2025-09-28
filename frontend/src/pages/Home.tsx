import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Home: React.FC = () => {
  const { user } = useAuth();

  return (
    <div className="home">
      <div className="hero">
        <h1>🏙️ Chicago O'Hare International Airport 🏙️</h1>
        <p>Your comprehensive flight management and booking platform</p>
        {!user && (
          <div className="cta-buttons">
            <Link to="/register" className="btn btn-primary">
              Get Started
            </Link>
            <Link to="/login" className="btn btn-secondary">
              Sign In
            </Link>
          </div>
        )}
      </div>
      
      <div className="features">
        <h2>✈️ Airport Services</h2>
        <div className="feature-grid">
          <div className="feature-card">
            <h3>📊 Real-time Flight Board</h3>
            <p>Track live flight information with real-time updates</p>
            <Link to="/flights" className="btn btn-primary">
              View Flights
            </Link>
          </div>
          <div className="feature-card">
            <h3>🎫 Flight Booking</h3>
            <p>Book flights directly from our comprehensive platform</p>
            <Link to="/flights" className="btn btn-primary">
              Book Flights
            </Link>
          </div>
          <div className="feature-card">
            <h3>📋 Check-in Services</h3>
            <p>Complete online check-in and receive boarding passes</p>
          </div>
          <div className="feature-card">
            <h3>📱 Flight Notifications</h3>
            <p>Receive real-time updates about your flight status</p>
          </div>
          {user && (
            <div className="feature-card">
              <h3>✈️ My Bookings</h3>
              <p>View and manage your flight reservations</p>
              <Link to="/my-bookings" className="btn btn-primary">
                View My Bookings
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Home;