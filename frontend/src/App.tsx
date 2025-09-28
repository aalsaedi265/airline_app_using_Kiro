import React from 'react';
import { Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { SignalRProvider } from './contexts/SignalRContext';
import Layout from './components/Layout/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import Home from './pages/Home';
import FlightBoard from './pages/FlightBoard';
import Login from './pages/Login';
import Register from './pages/Register';
import Booking from './pages/Booking';
import BookingCheckout from './pages/BookingCheckout';
import BookingConfirmation from './pages/BookingConfirmation';
import BookingSuccess from './pages/BookingSuccess';
import MyBookings from './pages/MyBookings';
import './App.css';

function App() {
  return (
    <AuthProvider>
      <SignalRProvider>
        <div className="App">
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<Home />} />
              <Route path="flights" element={<FlightBoard />} />
              <Route path="login" element={<Login />} />
              <Route path="register" element={<Register />} />

              {/* Protected booking routes */}
              <Route path="booking/:flightNumber" element={
                <ProtectedRoute>
                  <Booking />
                </ProtectedRoute>
              } />
              <Route path="booking-checkout/:flightNumber" element={
                <ProtectedRoute>
                  <BookingCheckout />
                </ProtectedRoute>
              } />
              <Route path="booking-confirmation/:confirmationNumber?" element={
                <ProtectedRoute>
                  <BookingConfirmation />
                </ProtectedRoute>
              } />
              <Route path="booking-success" element={
                <ProtectedRoute>
                  <BookingSuccess />
                </ProtectedRoute>
              } />
              <Route path="my-bookings" element={
                <ProtectedRoute>
                  <MyBookings />
                </ProtectedRoute>
              } />
            </Route>
          </Routes>
        </div>
      </SignalRProvider>
    </AuthProvider>
  );
}

export default App;