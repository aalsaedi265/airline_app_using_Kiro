# Environment Variables Setup

This project requires several environment variables to be set for proper operation. Create a `.env` file in the root directory with the following variables:

## Required Environment Variables

### Database Configuration
```bash
POSTGRES_DB=AirlineSimulationDb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_postgres_password
```

### JWT Configuration
```bash
JWT_SECRET=your-super-secret-jwt-key-that-is-at-least-32-characters-long
```

### External API Keys
```bash
AVIATION_STACK_API_KEY=your_aviation_stack_api_key_here
OPENWEATHER_API_KEY=your_openweather_api_key_here
```

### Redis Configuration (Optional)
```bash
REDIS_CONNECTION_STRING=localhost:6379
```

### Email Configuration (For Future Use)
```bash
SENDGRID_API_KEY=your_sendgrid_api_key_here
TWILIO_ACCOUNT_SID=your_twilio_account_sid_here
TWILIO_AUTH_TOKEN=your_twilio_auth_token_here
```

## How to Create the .env File

1. Create a new file named `.env` in the root directory of the project
2. Copy the variables above and replace the placeholder values with your actual credentials
3. Make sure the `.env` file is in your `.gitignore` (it already is)

## Example .env File

```bash
# Database Configuration
POSTGRES_DB=AirlineSimulationDb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=mypassword123

# JWT Configuration
JWT_SECRET=my-super-secret-jwt-key-that-is-at-least-32-characters-long-for-security

# External API Keys
AVIATION_STACK_API_KEY=abc123def456ghi789
OPENWEATHER_API_KEY=xyz789uvw456rst123

# Redis Configuration
REDIS_CONNECTION_STRING=localhost:6379
```

## Getting API Keys

### AviationStack API
1. Visit [AviationStack](https://aviationstack.com/)
2. Sign up for a free account
3. Get your API key from the dashboard

### OpenWeatherMap API
1. Visit [OpenWeatherMap](https://openweathermap.org/api)
2. Sign up for a free account
3. Get your API key from the API keys section

## Security Notes

- Never commit your `.env` file to version control
- Use strong, unique passwords for your database
- Use a long, random JWT secret (at least 32 characters)
- Keep your API keys secure and don't share them publicly
