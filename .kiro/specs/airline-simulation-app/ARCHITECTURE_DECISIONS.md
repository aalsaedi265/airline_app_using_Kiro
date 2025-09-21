# Architecture Decisions Record (ADR)

This document explains the technology choices made for the Airline Simulation Application and why alternatives were not selected.

## ADR-001: Use IMemoryCache instead of Redis

**Status**: Accepted  
**Date**: 2024-12-19  
**Context**: Need caching for flight data and weather information

### Decision
We chose ASP.NET Core's built-in `IMemoryCache` over Redis for caching.

### Rationale
- **Simplicity**: No external service dependencies
- **Learning Focus**: Easier to understand and debug
- **Sufficient Performance**: In-memory caching is fast enough for our use case
- **No Setup Required**: Works out of the box with ASP.NET Core

### Alternatives Considered
- **Redis**: More powerful, distributed caching, but adds complexity
- **SQL Server Memory-Optimized Tables**: Overkill for this application

### Consequences
- ✅ **Positive**: Simpler deployment, no Redis server to manage
- ✅ **Positive**: Faster development and testing
- ⚠️ **Negative**: Cache is lost when application restarts
- ⚠️ **Negative**: Not suitable for distributed scenarios

---

## ADR-002: Use BackgroundService instead of Hangfire

**Status**: Accepted  
**Date**: 2024-12-19  
**Context**: Need background jobs for flight status updates

### Decision
We chose ASP.NET Core's built-in `BackgroundService` over Hangfire for background job processing.

### Rationale
- **Simplicity**: No additional database tables or configuration
- **Built-in**: Part of ASP.NET Core, no external dependencies
- **Sufficient**: Our background jobs are simple and don't need advanced features
- **Learning**: Easier to understand for educational purposes

### Alternatives Considered
- **Hangfire**: Full-featured job scheduler with dashboard, but requires database setup
- **Quartz.NET**: More complex, overkill for simple scheduled tasks
- **Azure Functions**: Cloud-specific, not suitable for local development

### Consequences
- ✅ **Positive**: No additional database setup required
- ✅ **Positive**: Simpler configuration and deployment
- ⚠️ **Negative**: No job persistence across application restarts
- ⚠️ **Negative**: No job dashboard or monitoring UI

---

## ADR-003: Use Mock Data Services instead of External APIs

**Status**: Accepted  
**Date**: 2024-12-19  
**Context**: Need flight data and weather information

### Decision
We chose to create mock data services instead of integrating with external APIs like AviationStack and OpenWeatherMap.

### Rationale
- **No Dependencies**: No API keys, rate limits, or external service failures
- **Consistent Data**: Same results every time, perfect for demos
- **Always Available**: No external service downtime
- **Cost Effective**: No API costs or usage limits
- **Learning Focus**: Focus on application logic, not API integration

### Alternatives Considered
- **AviationStack API**: Real flight data, but requires API key and has rate limits
- **OpenWeatherMap API**: Real weather data, but requires API key and has usage limits
- **FlightAware AeroAPI**: Alternative flight data source, but still external dependency

### Consequences
- ✅ **Positive**: No external dependencies or API keys required
- ✅ **Positive**: Consistent, predictable data for demos
- ✅ **Positive**: No rate limiting or usage costs
- ⚠️ **Negative**: Data is not real-time or accurate
- ⚠️ **Negative**: Limited to generated scenarios

---

## ADR-004: Use JWT Authentication instead of ASP.NET Core Identity

**Status**: Accepted  
**Date**: 2024-12-19  
**Context**: Need user authentication and authorization

### Decision
We chose JWT (JSON Web Token) authentication over ASP.NET Core Identity.

### Rationale
- **Simplicity**: Easier to understand and implement
- **Stateless**: No server-side session storage required
- **Learning**: Focus on core authentication concepts
- **Sufficient**: Meets our basic authentication needs

### Alternatives Considered
- **ASP.NET Core Identity**: Full-featured identity system, but more complex
- **OAuth 2.0/OpenID Connect**: Industry standard, but overkill for this project
- **Cookie Authentication**: Simpler, but not suitable for API-only scenarios

### Consequences
- ✅ **Positive**: Simpler implementation and understanding
- ✅ **Positive**: Stateless, scalable authentication
- ⚠️ **Negative**: No built-in user management features
- ⚠️ **Negative**: No password reset or email verification

---

## ADR-005: Use PostgreSQL instead of SQL Server

**Status**: Accepted  
**Date**: 2024-12-19  
**Context**: Need a reliable database for the application

### Decision
We chose PostgreSQL over SQL Server as the primary database.

### Rationale
- **Familiarity**: Developer is already familiar with PostgreSQL
- **Open Source**: Free to use, no licensing costs
- **Cross-Platform**: Works on Windows, Mac, and Linux
- **EF Core Support**: Excellent Entity Framework Core support

### Alternatives Considered
- **SQL Server**: Microsoft's database, better .NET integration, but requires licensing
- **SQLite**: File-based database, simpler, but not suitable for production
- **MySQL**: Open source alternative, but less familiar to developer

### Consequences
- ✅ **Positive**: No licensing costs
- ✅ **Positive**: Cross-platform compatibility
- ✅ **Positive**: Developer familiarity
- ⚠️ **Negative**: Less .NET-specific tooling integration

---

## ADR-006: Use React Query instead of Redux

**Status**: Accepted  
**Date**: 2024-12-19  
**Context**: Need state management for API data

### Decision
We chose React Query over Redux for state management.

### Rationale
- **Purpose-Built**: Designed specifically for server state management
- **Simplicity**: Less boilerplate than Redux
- **Caching**: Built-in caching and synchronization
- **Learning**: Easier to understand for beginners

### Alternatives Considered
- **Redux**: Industry standard, but more complex for our needs
- **Zustand**: Simpler state management, but not focused on server state
- **Context API**: Built-in React solution, but requires more manual work

### Consequences
- ✅ **Positive**: Perfect for API state management
- ✅ **Positive**: Built-in caching and background updates
- ✅ **Positive**: Less boilerplate code
- ⚠️ **Negative**: Not suitable for complex client state management

---

## Summary

These architectural decisions prioritize **simplicity**, **learning**, and **maintainability** over advanced features and external dependencies. The chosen technologies provide a solid foundation for understanding full-stack development while keeping the complexity manageable for educational purposes.

Future developers can easily extend this architecture by:
- Adding Redis for distributed caching
- Implementing Hangfire for advanced job scheduling
- Integrating real external APIs
- Adding ASP.NET Core Identity for full user management
- Implementing Redux for complex client state

The current architecture serves as an excellent starting point that can be enhanced as needed.
