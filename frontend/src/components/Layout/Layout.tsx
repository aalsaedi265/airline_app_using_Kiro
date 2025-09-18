import React from 'react';
import { Outlet, Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import './Layout.css';

const Layout: React.FC = () => {
  const { user, logout } = useAuth();

  return (
    <div className="layout">
      <header className="header">
        <div className="container">
          <div className="nav-brand">
            <Link to="/">Airline Simulation</Link>
          </div>
          <nav className="nav">
            <Link to="/flights">Flight Board</Link>
            {user ? (
              <div className="user-menu">
                <span>Welcome, {user.firstName}</span>
                <button onClick={logout} className="btn btn-secondary">
                  Logout
                </button>
              </div>
            ) : (
              <div className="auth-links">
                <Link to="/login">Login</Link>
                <Link to="/register">Register</Link>
              </div>
            )}
          </nav>
        </div>
      </header>
      <main className="main">
        <div className="container">
          <Outlet />
        </div>
      </main>
      <footer className="footer">
        <div className="container">
          <p>&copy; 2024 Airline Simulation App. All rights reserved.</p>
        </div>
      </footer>
    </div>
  );
};

export default Layout;