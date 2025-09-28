import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Register: React.FC = () => {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [passwordStrength, setPasswordStrength] = useState({
    hasMinLength: false,
    hasUpperCase: false,
    hasLowerCase: false,
    hasNumber: false,
    hasSpecialChar: false
  });
  const [showPasswordRequirements, setShowPasswordRequirements] = useState(false);
  const [passwordsMatch, setPasswordsMatch] = useState(true);
  
  const { register } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value
    });

    // Clear error when user starts typing
    if (error) setError('');
  };

  // Password strength validation
  useEffect(() => {
    if (formData.password) {
      setPasswordStrength({
        hasMinLength: formData.password.length >= 6,
        hasUpperCase: /[A-Z]/.test(formData.password),
        hasLowerCase: /[a-z]/.test(formData.password),
        hasNumber: /\d/.test(formData.password),
        hasSpecialChar: /[!@#$%^&*(),.?":{}|<>]/.test(formData.password)
      });
    }
  }, [formData.password]);

  // Password confirmation matching
  useEffect(() => {
    if (formData.confirmPassword) {
      setPasswordsMatch(formData.password === formData.confirmPassword);
    }
  }, [formData.password, formData.confirmPassword]);

  const isPasswordValid = () => {
    return passwordStrength.hasMinLength &&
           passwordStrength.hasLowerCase &&
           passwordStrength.hasNumber;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    // Validation checks
    if (!formData.firstName.trim() || !formData.lastName.trim()) {
      setError('First name and last name are required');
      setIsLoading(false);
      return;
    }

    if (!formData.email.trim() || !formData.email.includes('@')) {
      setError('Please enter a valid email address');
      setIsLoading(false);
      return;
    }

    if (!isPasswordValid()) {
      setError('Password must be at least 6 characters with lowercase and number');
      setIsLoading(false);
      return;
    }

    if (!passwordsMatch) {
      setError('Passwords do not match');
      setIsLoading(false);
      return;
    }

    try {
      const success = await register({
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        password: formData.password
      });
      
      if (success) {
        navigate('/');
      } else {
        setError('Registration failed. Please try again.');
      }
    } catch (error: any) {
      // More specific error handling
      if (error.message.includes('already exists')) {
        setError('This email is already registered. Please use a different email or try logging in.');
      } else if (error.message.includes('Invalid email')) {
        setError('Please enter a valid email address.');
      } else if (error.message.includes('Password')) {
        setError('Password does not meet requirements.');
      } else {
        setError(error.message || 'Registration failed. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-form">
        <h2>Create Account</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="firstName">First Name</label>
              <input
                type="text"
                id="firstName"
                name="firstName"
                value={formData.firstName}
                onChange={handleChange}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="lastName">Last Name</label>
              <input
                type="text"
                id="lastName"
                name="lastName"
                value={formData.lastName}
                onChange={handleChange}
                required
              />
            </div>
          </div>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              onFocus={() => setShowPasswordRequirements(true)}
              onBlur={() => setShowPasswordRequirements(false)}
              required
            />
            {showPasswordRequirements && (
              <div className="password-requirements">
                <div className="requirement-title">Password Requirements:</div>
                <div className={`requirement ${passwordStrength.hasMinLength ? 'met' : 'unmet'}`}>
                  ✓ At least 6 characters
                </div>
                <div className={`requirement ${passwordStrength.hasLowerCase ? 'met' : 'unmet'}`}>
                  ✓ At least one lowercase letter
                </div>
                <div className={`requirement ${passwordStrength.hasNumber ? 'met' : 'unmet'}`}>
                  ✓ At least one number
                </div>
                <div className={`requirement ${passwordStrength.hasUpperCase ? 'met' : 'recommended'}`}>
                  ~ Uppercase letter (recommended)
                </div>
                <div className={`requirement ${passwordStrength.hasSpecialChar ? 'met' : 'recommended'}`}>
                  ~ Special character (recommended)
                </div>
              </div>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="confirmPassword">Confirm Password</label>
            <input
              type="password"
              id="confirmPassword"
              name="confirmPassword"
              value={formData.confirmPassword}
              onChange={handleChange}
              className={formData.confirmPassword && !passwordsMatch ? 'error' : ''}
              required
            />
            {formData.confirmPassword && !passwordsMatch && (
              <div className="field-error">Passwords do not match</div>
            )}
          </div>
          {error && <div className="error-message">{error}</div>}
          <button type="submit" disabled={isLoading} className="btn btn-primary">
            {isLoading ? 'Creating Account...' : 'Create Account'}
          </button>
        </form>
        <p>
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  );
};

export default Register;