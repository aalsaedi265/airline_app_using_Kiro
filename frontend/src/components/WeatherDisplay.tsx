import React from 'react';
import './WeatherDisplay.css';

interface WeatherInfo {
  temperature: number;
  conditions: string;
  humidity: number;
  windSpeed: number;
  windDirection: string;
  visibility: number;
}

interface WeatherDisplayProps {
  title: string;
  location: string;
  weather: WeatherInfo | null;
  isLoading?: boolean;
  error?: string;
}

const WeatherDisplay: React.FC<WeatherDisplayProps> = ({
  title,
  location,
  weather,
  isLoading = false,
  error
}) => {
  const getWeatherIcon = (conditions: string) => {
    const condition = conditions.toLowerCase();
    if (condition.includes('clear') || condition.includes('sunny')) return '☀️';
    if (condition.includes('cloud')) return '☁️';
    if (condition.includes('rain')) return '🌧️';
    if (condition.includes('snow')) return '❄️';
    if (condition.includes('thunder') || condition.includes('storm')) return '⛈️';
    if (condition.includes('fog') || condition.includes('mist')) return '🌫️';
    return '🌤️';
  };

  const getPackingSuggestions = (weather: WeatherInfo) => {
    const suggestions = [];

    if (weather.temperature < 5) {
      suggestions.push('🧥 Heavy winter coat');
      suggestions.push('🧤 Gloves and warm accessories');
    } else if (weather.temperature < 15) {
      suggestions.push('🧥 Jacket or sweater');
      suggestions.push('👖 Long pants');
    } else if (weather.temperature > 25) {
      suggestions.push('👕 Light clothing');
      suggestions.push('🕶️ Sunglasses');
      suggestions.push('🧴 Sunscreen');
    }

    if (weather.conditions.toLowerCase().includes('rain')) {
      suggestions.push('☂️ Umbrella or rain jacket');
    }

    if (weather.windSpeed > 20) {
      suggestions.push('🧥 Windproof jacket');
    }

    return suggestions;
  };

  if (isLoading) {
    return (
      <div className="weather-display loading">
        <h3>{title}</h3>
        <div className="weather-loading">
          <div className="weather-spinner"></div>
          <p>Loading weather for {location}...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="weather-display error">
        <h3>{title}</h3>
        <div className="weather-error">
          <p>⚠️ Weather data unavailable for {location}</p>
          <small>{error}</small>
        </div>
      </div>
    );
  }

  if (!weather) {
    return (
      <div className="weather-display no-data">
        <h3>{title}</h3>
        <p>No weather data available for {location}</p>
      </div>
    );
  }

  const packingSuggestions = getPackingSuggestions(weather);

  return (
    <div className="weather-display">
      <h3>{title}</h3>
      <div className="weather-location">
        <span className="location-name">{location}</span>
      </div>

      <div className="weather-main">
        <div className="weather-icon">
          {getWeatherIcon(weather.conditions)}
        </div>
        <div className="weather-temp">
          <span className="temperature">{Math.round(weather.temperature)}°C</span>
          <span className="conditions">{weather.conditions}</span>
        </div>
      </div>

      <div className="weather-details">
        <div className="weather-detail">
          <span className="label">💧 Humidity</span>
          <span className="value">{weather.humidity}%</span>
        </div>
        <div className="weather-detail">
          <span className="label">💨 Wind</span>
          <span className="value">{Math.round(weather.windSpeed)} km/h {weather.windDirection}</span>
        </div>
        <div className="weather-detail">
          <span className="label">👁️ Visibility</span>
          <span className="value">{weather.visibility} km</span>
        </div>
      </div>

      {packingSuggestions.length > 0 && (
        <div className="packing-suggestions">
          <h4>📦 Packing Suggestions</h4>
          <ul>
            {packingSuggestions.map((suggestion, index) => (
              <li key={index}>{suggestion}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default WeatherDisplay;