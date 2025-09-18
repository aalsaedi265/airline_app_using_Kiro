import React from 'react';
import { useSignalR } from '../contexts/SignalRContext';

const FlightBoard: React.FC = () => {
  const { isConnected } = useSignalR();

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
        />
        <select className="filter-select">
          <option value="">All Airlines</option>
          <option value="AA">American Airlines</option>
          <option value="UA">United Airlines</option>
          <option value="DL">Delta Airlines</option>
        </select>
      </div>

      <div className="flight-table">
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
            <tr>
              <td colSpan={8} className="no-data">
                Flight data will be loaded in future tasks
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default FlightBoard;