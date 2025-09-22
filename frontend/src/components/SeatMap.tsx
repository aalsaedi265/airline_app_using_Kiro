import React, { useState, useEffect } from 'react';
import { apiService, SeatMap as SeatMapType, Seat } from '../services/api';

interface SeatMapProps {
  flightNumber: string;
  flightDate: string;
  onSeatSelect: (seatNumber: string) => void;
  selectedSeats: string[];
  seatClass: string;
}

const SeatMap: React.FC<SeatMapProps> = ({
  flightNumber,
  flightDate,
  onSeatSelect,
  selectedSeats,
  seatClass
}) => {
  const [seatMap, setSeatMap] = useState<SeatMapType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadSeatMap();
  }, [flightNumber, flightDate]);

  const loadSeatMap = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getSeatMap(flightNumber, new Date(flightDate));
      setSeatMap(data);
    } catch (err) {
      setError('Failed to load seat map');
      console.error('Error loading seat map:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSeatClick = (seat: Seat) => {
    if (seat.isAvailable) {
      onSeatSelect(seat.number);
    }
  };

  const getSeatClass = (seat: Seat) => {
    if (!seat.isAvailable) return 'seat-unavailable';
    if (selectedSeats.includes(seat.number)) return 'seat-selected';
    if (seat.class.toLowerCase() === seatClass.toLowerCase()) return 'seat-available';
    return 'seat-other-class';
  };

  const getSeatLabel = (seat: Seat) => {
    if (!seat.isAvailable) return 'X';
    if (selectedSeats.includes(seat.number)) return '✓';
    return seat.number;
  };

  if (loading) {
    return <div className="seat-map-loading">Loading seat map...</div>;
  }

  if (error) {
    return <div className="seat-map-error">{error}</div>;
  }

  if (!seatMap) {
    return <div className="seat-map-error">No seat map available</div>;
  }

  return (
    <div className="seat-map">
      <div className="seat-map-header">
        <h3>Select Your Seats</h3>
        <p>Flight {flightNumber} - {new Date(flightDate).toLocaleDateString()}</p>
        <div className="seat-legend">
          <div className="legend-item">
            <div className="seat seat-available"></div>
            <span>Available ({seatClass})</span>
          </div>
          <div className="legend-item">
            <div className="seat seat-selected"></div>
            <span>Selected</span>
          </div>
          <div className="legend-item">
            <div className="seat seat-unavailable"></div>
            <span>Unavailable</span>
          </div>
          <div className="legend-item">
            <div className="seat seat-other-class"></div>
            <span>Other Class</span>
          </div>
        </div>
      </div>

      <div className="aircraft-layout">
        <div className="aircraft-nose">✈</div>
        
        {seatMap.rows.map((row) => (
          <div key={row.rowNumber} className="seat-row">
            <div className="row-number">{row.rowNumber}</div>
            <div className="seats">
              {row.seats.map((seat) => (
                <div
                  key={seat.number}
                  className={`seat ${getSeatClass(seat)}`}
                  onClick={() => handleSeatClick(seat)}
                  title={`${seat.number} - ${seat.class}`}
                >
                  {getSeatLabel(seat)}
                </div>
              ))}
            </div>
            <div className="row-number">{row.rowNumber}</div>
          </div>
        ))}
        
        <div className="aircraft-tail">✈</div>
      </div>

      <div className="seat-map-footer">
        <p>Click on available seats to select them</p>
        {selectedSeats.length > 0 && (
          <div className="selected-seats">
            <strong>Selected: {selectedSeats.join(', ')}</strong>
          </div>
        )}
      </div>
    </div>
  );
};

export default SeatMap;
