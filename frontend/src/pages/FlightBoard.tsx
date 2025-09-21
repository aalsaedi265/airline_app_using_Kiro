import React, { useState, useEffect } from 'react';
import { useSignalR } from '../contexts/SignalRContext';
import { apiService, FlightSummary } from '../services/api';

const FlightBoard: React.FC = () => {
  const { isConnected } = useSignalR();
  const [flights, setFlights] = useState<FlightSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedAirline, setSelectedAirline] = useState('');

  useEffect(() => {
    loadFlights();
  }, []);

  const loadFlights = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiService.getFlightBoard('ORD', searchTerm || undefined, undefined, selectedAirline || undefined);
      setFlights(response.flights);
    } catch (err) {
      setError('Failed to load flight data');
      console.error('Error loading flights:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    loadFlights();
  };

  const formatTime = (timeString: string) => {
    return new Date(timeString).toLocaleTimeString('en-US', { 
      hour: '2-digit', 
      minute: '2-digit',
      hour12: false 
    });
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'on time':
        return 'status-on-time';
      case 'delayed':
        return 'status-delayed';
      case 'boarding':
        return 'status-boarding';
      case 'cancelled':
        return 'status-cancelled';
      default:
        return 'status-unknown';
    }
  };

  return (
    <div className="flight-board">
      <div className="flight-board-header">
        <h1>Flight Board - Chicago O'Hare (ORD)</h1>
        <div className="connection-status">
          Status: {isConnected ? 'Connected' : 'Disconnected'}
        </div>
      </div>
      
      <div className="flight-filters">
        <input 
          type="text" 
          placeholder="Search flights..." 
          className="search-input"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
        <select 
          className="filter-select"
          value={selectedAirline}
          onChange={(e) => setSelectedAirline(e.target.value)}
        >
          <option value="">All Airlines</option>
          <option value="American">American Airlines</option>
          <option value="United">United Airlines</option>
          <option value="Delta">Delta Airlines</option>
        </select>
        <button onClick={handleSearch} className="search-button">
          Search
        </button>
      </div>

      <div className="flight-table">
        {loading ? (
          <div className="loading">Loading flight data...</div>
        ) : error ? (
          <div className="error">{error}</div>
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
              </tr>
            </thead>
            <tbody>
              {flights.length === 0 ? (
                <tr>
                  <td colSpan={8} className="no-data">
                    No flights found
                  </td>
                </tr>
              ) : (
                flights.map((flight) => (
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