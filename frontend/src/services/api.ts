const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

class ApiService {
  private getAuthToken(): string | null {
    return localStorage.getItem('authToken');
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    const token = this.getAuthToken();

    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
        ...options.headers,
      },
      ...options,
    };

    const response = await fetch(url, config);
    
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'An error occurred' }));
      throw new Error(error.message || 'Request failed');
    }

    return response.json();
  }

  // Auth endpoints
  async login(email: string, password: string) {
    return this.request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  }

  async register(email: string, firstName: string, lastName: string, password: string) {
    return this.request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, firstName, lastName, password }),
    });
  }

  // Flight endpoints
  async getFlightBoard(airport: string = 'ORD', search?: string, status?: string, airline?: string) {
    const params = new URLSearchParams({ airport });
    if (search) params.append('search', search);
    if (status) params.append('status', status);
    if (airline) params.append('airline', airline);
    
    return this.request<FlightBoardResponse>(`/flights/board?${params}`);
  }

  async getFlightDetails(flightNumber: string, date?: Date) {
    const params = new URLSearchParams();
    if (date) params.append('date', date.toISOString());
    
    return this.request<FlightDetailsResponse>(`/flights/${flightNumber}?${params}`);
  }

  async getFlightWeather(flightNumber: string, date?: Date) {
    const params = new URLSearchParams();
    if (date) params.append('date', date.toISOString());
    
    return this.request<WeatherResponse>(`/flights/${flightNumber}/weather?${params}`);
  }

  // Booking endpoints
  async createBooking(booking: CreateBookingRequest) {
    return this.request<BookingResponse>('/bookings', {
      method: 'POST',
      body: JSON.stringify(booking),
    });
  }

  async getBooking(confirmationNumber: string) {
    return this.request<BookingDetailsResponse>(`/bookings/${confirmationNumber}`);
  }

  async checkIn(confirmationNumber: string) {
    return this.request<CheckInResponse>(`/bookings/${confirmationNumber}/checkin`, {
      method: 'POST',
    });
  }

  async getSeatMap(flightNumber: string, date: Date) {
    return this.request<SeatMapType>(`/flights/${flightNumber}/seats?date=${date.toISOString()}`);
  }
}

export const apiService = new ApiService();

// Types
export interface AuthResponse {
  token: string;
  user: UserDto;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface FlightBoardResponse {
  airport: string;
  flights: FlightSummary[];
  lastUpdated: string;
  totalCount: number;
}

export interface FlightDetailsResponse {
  flight: FlightDetails;
  lastUpdated: string;
}

export interface WeatherResponse {
  flightNumber: string;
  originAirport: string;
  destinationAirport: string;
  originWeather?: WeatherInfo;
  destinationWeather?: WeatherInfo;
  lastUpdated: string;
}

export interface FlightSummary {
  id: number;
  flightNumber: string;
  airline: string;
  originAirport: string;
  destinationAirport: string;
  scheduledDeparture: string;
  estimatedDeparture?: string;
  scheduledArrival: string;
  estimatedArrival?: string;
  status: string;
  gate?: string;
  terminal?: string;
}

export interface FlightDetails extends FlightSummary {
  aircraft?: string;
  createdAt: string;
  updatedAt: string;
}

export interface WeatherInfo {
  temperature: number;
  conditions: string;
  humidity: number;
  windSpeed: number;
  windDirection: string;
  visibility: number;
}

export interface CreateBookingRequest {
  flightNumber: string;
  flightDate: string;
  passengers: PassengerRequest[];
  selectedSeats: string[];
}

export interface PassengerRequest {
  firstName: string;
  lastName: string;
  dateOfBirth?: string;
  seatClass: string;
}

export interface BookingResponse {
  confirmationNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
}

export interface BookingDetailsResponse {
  confirmationNumber: string;
  status: string;
  totalAmount: number;
  flight: FlightSummary;
  passengers: PassengerDto[];
  createdAt: string;
}

export interface CheckInResponse {
  success: boolean;
  boardingPass?: BoardingPass;
}

export interface PassengerDto {
  firstName: string;
  lastName: string;
  seatNumber?: string;
  seatClass: string;
}

export interface BoardingPass {
  confirmationNumber: string;
  passengerName: string;
  flightNumber: string;
  seatNumber: string;
  gate: string;
  boardingTime: string;
  qrCode: string;
}

export interface SeatMapType {
  flightNumber: string;
  rows: SeatRow[];
}

export type SeatMap = SeatMapType;

export interface SeatRow {
  rowNumber: number;
  seats: Seat[];
}

export interface Seat {
  number: string;
  class: string;
  isAvailable: boolean;
}
