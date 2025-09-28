import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSignalR } from '../contexts/SignalRContext';
import { useAuth } from '../contexts/AuthContext';
import { apiService, FlightSummary } from '../services/api';

const FlightBoard: React.FC = () => {
  const { isConnected } = useSignalR();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [allFlights, setAllFlights] = useState<FlightSummary[]>([]);
  const [filteredFlights, setFilteredFlights] = useState<FlightSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedAirline, setSelectedAirline] = useState('');

  useEffect(() => {
    loadFlights();
  }, []);

  // Load all flights once
  const loadFlights = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiService.getFlightBoard('ORD');
      setAllFlights(response.flights);
      setFilteredFlights(response.flights);
    } catch (err) {
      setError('Failed to load flight data');
      console.error('Error loading flights:', err);
    } finally {
      setLoading(false);
    }
  };

  // Dynamic filtering based on search term and airline
  useEffect(() => {
    let filtered = allFlights;

    // Filter by search term (flight number, airline, destination)
    if (searchTerm.trim()) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(flight => 
        flight.flightNumber.toLowerCase().includes(term) ||
        flight.airline.toLowerCase().includes(term) ||
        flight.destinationAirport.toLowerCase().includes(term) ||
        flight.originAirport.toLowerCase().includes(term)
      );
    }

    // Filter by airline
    if (selectedAirline) {
      filtered = filtered.filter(flight => 
        flight.airline.toLowerCase().includes(selectedAirline.toLowerCase())
      );
    }

    setFilteredFlights(filtered);
  }, [allFlights, searchTerm, selectedAirline]);

  const formatTime = (timeString: string) => {
    return new Date(timeString).toLocaleTimeString('en-US', { 
      hour: '2-digit', 
      minute: '2-digit',
      hour12: false 
    });
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'ontime':
        return 'status-on-time';
      case 'delayed':
        return 'status-delayed';
      case 'boarding':
        return 'status-boarding';
      case 'cancelled':
        return 'status-cancelled';
      case 'scheduled':
        return 'status-on-time';
      case 'departed':
        return 'status-departed';
      case 'arrived':
        return 'status-arrived';
      default:
        return 'status-unknown';
    }
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
  };

  const handleAirlineChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSelectedAirline(e.target.value);
  };

  // Get unique airlines for the dropdown
  const uniqueAirlines = Array.from(new Set(allFlights.map(flight => flight.airline))).sort();

  const handleBookFlight = (flight: FlightSummary) => {
    if (!user) {
      // Store the intended booking flight in session storage for after login
      sessionStorage.setItem('intendedBookingFlight', JSON.stringify({
        flight,
        flightDate: new Date().toISOString().split('T')[0]
      }));
      navigate('/login', {
        state: {
          from: { pathname: `/booking-checkout/${flight.flightNumber}` }
        }
      });
      return;
    }
    navigate(`/booking-checkout/${flight.flightNumber}`, {
      state: {
        flight,
        flightDate: new Date().toISOString().split('T')[0]
      }
    });
  };

  return (
    <div className="flight-board">
      <div className="flight-board-header">
        <h1>🏙️ Flight Information Display - Chicago O'Hare (ORD) 🏙️</h1>
        <div className="connection-status">
          {isConnected ? '🟢 System Connected' : '🔴 System Disconnected'}
        </div>
        {!user && (
          <div className="auth-notice" style={{
            background: '#e3f2fd',
            padding: '12px',
            borderRadius: '8px',
            marginTop: '10px',
            textAlign: 'center',
            border: '1px solid #bbdefb'
          }}>
            📝 To book flights, please <a href="/login" style={{color: '#1976d2', fontWeight: 'bold'}}>Login</a> or <a href="/register" style={{color: '#1976d2', fontWeight: 'bold'}}>Register</a>
          </div>
        )}
      </div>
      
      <div className="flight-filters">
        <input 
          type="text" 
          placeholder="🔍 Search flights by number, airline, or destination..." 
          className="search-input"
          value={searchTerm}
          onChange={handleSearchChange}
        />
        <select 
          className="filter-select"
          value={selectedAirline}
          onChange={handleAirlineChange}
        >
          <option value="">All Airlines</option>
          {uniqueAirlines.map(airline => (
            <option key={airline} value={airline}>{airline}</option>
          ))}
        </select>
        <div className="filter-info">
          ✈️ Flight Status: {filteredFlights.length} of {allFlights.length} flights
        </div>
      </div>

      <div className="flight-table">
        {loading ? (
          <div className="loading">✈️ Loading flight information...</div>
        ) : error ? (
          <div className="error">⚠️ {error}</div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Flight</th>
                <th>Airline</th>
                <th>Destination</th>
                <th>Scheduled</th>
                <th>Estimated</th>
                <th>Status</th>
                <th>Gate</th>
                <th>Terminal</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {filteredFlights.length === 0 ? (
                <tr>
                  <td colSpan={9} className="no-data">
                    {allFlights.length === 0 ? '⚓ No flights available' : '🔍 No flights match your search criteria'}
                  </td>
                </tr>
              ) : (
                filteredFlights.map((flight) => (
                  <tr key={flight.id}>
                    <td>{flight.flightNumber}</td>
                    <td>{flight.airline}</td>
                    <td>{flight.destinationAirport}</td>
                    <td>{formatTime(flight.scheduledDeparture)}</td>
                    <td>{flight.estimatedDeparture ? formatTime(flight.estimatedDeparture) : '-'}</td>
                    <td>
                      <span className={`status ${getStatusColor(flight.status)}`}>
                        {flight.status}
                      </span>
                    </td>
                    <td>{flight.gate || '-'}</td>
                    <td>{flight.terminal || '-'}</td>
                    <td>
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleBookFlight(flight)}
                        disabled={flight.status === 'Cancelled' || flight.status === 'Departed'}
                      >
                        {user ? 'Book Flight' : 'Book Now (Login Required)'}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default FlightBoard;