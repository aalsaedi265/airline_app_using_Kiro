import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Home: React.FC = () => {
  const { user } = useAuth();

  return (
    <div className="home">
      <div className="hero">
        <h1>Welcome to Airline Simulation</h1>
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
        <h2>Features</h2>
        <div className="feature-grid">
          <div className="feature-card">
            <h3>Real-time Flight Board</h3>
            <p>Track live flight information with real-time updates</p>
            <Link to="/flights" className="btn btn-primary">
              View Flights
            </Link>
          </div>
          <div className="feature-card">
            <h3>Flight Booking</h3>
            <p>Book flights directly from the flight board</p>
          </div>
          <div className="feature-card">
            <h3>Check-in Services</h3>
            <p>Complete online check-in and get boarding passes</p>
          </div>
          <div className="feature-card">
            <h3>Notifications</h3>
            <p>Receive real-time updates about your flights</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Home;