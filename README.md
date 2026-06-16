# iAdmin - Windows Administrator Automation Platform

![Version](https://img.shields.io/badge/version-1.0.0--dev-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-Proprietary-red)

## Overview

iAdmin is a Windows desktop application for secure administrator mode authentication and software management without repeated password prompts. The application uses JWT-based authentication, BCrypt password hashing, and automatic background updates.

## Architecture

### Project Structure
- **iAdmin.Common** - Shared models, DTOs, and enums
- **iAdmin.Data** - Entity Framework Core data access layer
- **iAdmin.Server** - ASP.NET Core Web API backend
- **iAdmin.Client** - WPF desktop application

### Key Features
✅ JWT-based admin authentication
✅ Secure password hashing (BCrypt cost 12)
✅ Local SQLite persistence with async API sync
✅ Comprehensive audit logging
✅ MVVM architecture for WPF
✅ Structured logging with Serilog
✅ Entity Framework Core migrations
✅ Refresh token rotation

## Prerequisites

- Windows 10 or later
- .NET 10 SDK ([Download](https://dotnet.microsoft.com/download))
- Visual Studio Code or Visual Studio 2022+ (optional)

## Getting Started

### 1. Build the Solution
```bash
cd d:\Projects\LubSoft\iAdmin
dotnet build
```

### 2. Database Setup
```bash
cd iAdmin.Server
dotnet ef database update
```

This creates the SQLite database at `%APPDATA%\iAdmin\iAdmin.db` and seeds a default admin user:
- **Username**: `admin`
- **Password**: `admin123`

### 3. Start the Server
```bash
cd iAdmin.Server
dotnet run
```

Server runs on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### 4. Start the Client
```bash
cd iAdmin.Client
.\Run-ClientAsAdmin.ps1 -Build
```

If script execution is blocked, use:
```bash
powershell -ExecutionPolicy Bypass -File .\Run-ClientAsAdmin.ps1 -Build
```

### 5. Login
- Username: `admin`
- Password: `admin123`

## Configuration

### Server Configuration (appsettings.json)

```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-change-this-in-production-minimum-32-characters-long!",
    "Issuer": "iAdmin",
    "Audience": "iAdmin-client",
    "ExpiryMinutes": 15,
    "RefreshExpiryDays": 7
  }
}
```

**IMPORTANT**: Change JWT secret key in production!

### Client Configuration

Token storage location: `%APPDATA%\iAdmin\.tokens`
Log files: `%APPDATA%\iAdmin\Logs\`

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/refresh` - Refresh access token

### Updates
- `GET /api/updates/latest` - Get latest available update
- `GET /api/updates/download/{version}` - Download update package

## Database Schema

### Tables
- **Users** - User credentials and metadata
- **AdminSessions** - Admin session history
- **UpdateHistory** - Applied updates tracking
- **AuditLog** - Complete audit trail of all operations

## Logging

### Serilog Configuration
- **Level**: Information (configurable)
- **Location**: `%APPDATA%\iAdmin\Logs\`
- **Format**: JSON structured logs with timestamps

### Sample Log Entry
```json
{
  "Timestamp": "2026-06-16 10:30:45.123 +02:00",
  "Level": "Information",
  "Message": "User logged in successfully",
  "UserId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## Security

### Client-Side
- Tokens stored encrypted locally
- HTTPS communication enforced
- Token rotation on refresh
- No password caching

### Server-Side
- BCrypt hashing with cost 12+
- 15-minute access token expiry
- 7-day refresh token expiry
- Rate limiting on auth endpoints
- CORS restricted to trusted origins
- Immutable audit logs

## Development Workflow

### Adding a new API Endpoint
1. Create DTO in `iAdmin.Common/Dtos/`
2. Create controller in `iAdmin.Server/Controllers/`
3. Add repository method if needed
4. Document in API controller XML comments

### Adding Database Changes
```bash
cd iAdmin.Server
dotnet ef migrations add DescriptionOfChange -p ../iAdmin.Data/iAdmin.Data.csproj
dotnet ef database update
```

### Troubleshooting

**Connection Refused on https://localhost:5001**
- Ensure server is running
- Check firewall settings
- Verify certificate is trusted (development cert)

**Database Not Found**
- Run `dotnet ef database update` in iAdmin.Server
- Check `%APPDATA%\iAdmin\iAdmin.db` exists

**Login Fails**
- Verify default admin user exists (check database)
- Re-seed if needed using DataSeederService
- Check logs in `%APPDATA%\iAdmin\Logs\`

## Documentation

Detailed technical documentation available in [.vscode/documentation/](/.vscode/documentation/)

- [ARCHITECTURE.md](/.vscode/documentation/ARCHITECTURE.md) - System architecture and design
- Project structure and conventions
- API contract specifications

## Testing

### Manual Testing Checklist
- [ ] Build compiles without errors
- [ ] Database migrations apply successfully
- [ ] Server starts without errors
- [ ] Client launches and connects to server
- [ ] Login with admin/admin123 succeeds
- [ ] Token refresh works
- [ ] Audit logs are created

### Running Tests
```bash
dotnet test
```

## Roadmap

### Phase 1 (Current)
- ✅ Basic admin login system
- ✅ JWT authentication
- ✅ Database persistence
- 🔄 Auto-update mechanism

### Phase 2
- [ ] OAuth/SAML integration
- [ ] Multi-factor authentication (TOTP)
- [ ] Remote deployment
- [ ] Batch operations

### Phase 3
- [ ] PowerShell script execution
- [ ] System event monitoring
- [ ] Advanced audit reports
- [ ] Mobile companion app

## Maintenance

### Regular Tasks
1. Monitor log files for errors
2. Review audit logs for security incidents
3. Update dependencies monthly
4. Backup database regularly

### Security Updates
- Subscribe to .NET security updates
- Update NuGet packages: `dotnet outdated`
- Review dependency vulnerabilities

## Support & Issues

For issues and feature requests, please refer to project documentation or contact the development team.

## License

Proprietary - LubSoft Internal Use Only

---

**Version**: 1.0.0-dev  
**Last Updated**: June 16, 2026  
**Maintainer**: Architecture Team
