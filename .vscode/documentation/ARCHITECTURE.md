# iAdmin Architecture

## Overview
iAdmin is a Windows administrative automation platform enabling elevated privilege operations (software uninstallation, system configuration) without repeated password prompts. The system uses JWT-based authentication with automatic background updates.

## Project Structure

### 1. **iAdmin.Common** (Class Library)
- **Purpose**: Shared models, DTOs, and utilities
- **Contents**:
  - `Models/`: Domain entities (User, AdminSession, UpdatePackage, etc.)
  - `Dtos/`: Data Transfer Objects for API communication
  - `Enums/`: Status enums (AdminSessionStatus, UpdateStatus)
  - `Constants/`: Application constants and configuration keys
  - `Exceptions/`: Custom exception types
  - `Extensions/`: Utility extension methods

### 2. **iAdmin.Data** (Class Library)
- **Purpose**: Data access and persistence layer
- **Contents**:
  - `Context/`: Entity Framework DbContext (iAdminDbContext)
  - `Entities/`: EF Core entity configurations
  - `Repositories/`: Data access patterns (Repository pattern)
  - `Migrations/`: EF Core database migrations
- **Persistence**: SQLite database for local offline data
- **Connection**: Local file-based storage at `%APPDATA%/iAdmin/iAdmin.db`

### 3. **iAdmin.Server** (ASP.NET Core Web API)
- **Purpose**: Backend API and update service
- **Ports**: 
  - HTTP: 5000
  - HTTPS: 5001
- **Endpoints**:
  - `POST /api/auth/login` - Authenticate user (credentials verification)
  - `POST /api/auth/refresh` - Refresh JWT token
  - `GET /api/updates/latest` - Get latest update package info
  - `GET /api/updates/download/{version}` - Download update package
  - `POST /api/audit/log` - Log audit events
- **Authentication**: JWT Bearer tokens with refresh token rotation
- **CORS**: Allow iAdmin.Client localhost origins

### 4. **iAdmin.Client** (WPF Application)
- **Purpose**: Desktop UI for admin operations
- **Architecture**: MVVM pattern
- **Views**:
  - LoginWindow: Initial authentication
  - AdminSessionWindow: Active admin session management
  - UpdateWindow: Update status and progress
- **Services**:
  - AuthenticationService: JWT token management
  - AdminElevationService: Windows elevation API wrapper
  - UpdateService: Check and apply updates
  - AuditService: Local audit logging

## Authentication Flow

```
1. User enters credentials
   ↓
2. LoginWindow → AuthenticationService.LoginAsync()
   ↓
3. Request: POST /api/auth/login (username, password)
   ↓
4. Server validates credentials (BCrypt comparison)
   ↓
5. Server generates JWT + Refresh Token
   ↓
6. Client stores tokens in secure local storage
   ↓
7. User gains admin session access
   ↓
8. On token expiry: Use refresh token → /api/auth/refresh
```

## Admin Session Model

- **Duration**: Configurable (default: 1 hour)
- **Re-elevation**: Users can request new session without re-login
- **Audit Trail**: All admin actions logged with:
  - User ID (GUID)
  - Action type
  - Timestamp (UTC)
  - Old/New values for changes
  - Status (Success/Failure)

## Auto-Update System

### Update Flow
```
1. Client starts → UpdateService.CheckForUpdatesAsync()
2. Request: GET /api/updates/latest
3. Server returns available version info (if newer than local)
4. User prompted to update OR auto-applies if configured
5. Request: GET /api/updates/download/{version}
6. Package downloaded to `%APPDATA%/iAdmin/Updates/`
7. Verify hash (SHA256)
8. Extract and apply update
9. Restart application or background service
```

### Update Package Structure
- `manifest.json` - Version info, hash, dependencies
- `bin/` - Updated binaries
- `migrations.sql` - Optional database migrations
- `signature.bin` - Cryptographic signature (optional)

## Database Schema

### Tables
- **Users**: Local user cache (UserId, Username, Email, PasswordHash, LastLogin, CreatedAt)
- **AdminSessions**: Active/historical sessions (SessionId, UserId, StartTime, EndTime, Status, IpAddress)
- **UpdateHistory**: Applied updates (UpdateId, Version, AppliedAt, Status, Checksum)
- **AuditLog**: All privileged operations (AuditId, UserId, Action, OldValue, NewValue, ChangedAt, ChangedBy)

### Indexes
- `Users.Username` - Unique
- `AdminSessions.UserId, StartTime`
- `AuditLog.ChangedAt DESC` - Recent first
- `UpdateHistory.AppliedAt DESC`

## Security Baseline

### Client-Side
- Tokens stored in encrypted local settings
- Passwords never cached (entered per session)
- HTTPS only for API communication
- Token rotation on refresh

### Server-Side
- Passwords hashed with BCrypt (cost 12+)
- JWT with 15-minute expiry
- Refresh tokens with 7-day expiry
- Rate limiting on `/api/auth/login`
- CORS restricted to trusted origins
- Input validation on all endpoints

### Audit & Compliance
- All operations logged with timestamps
- User identity capture (Windows principal)
- Change tracking (old/new values per field)
- Immutable audit log (no deletes)

## Development Workflow

### Build
```bash
dotnet build iAdmin.slnx
```

### Run Server
```bash
cd iAdmin.Server
dotnet run
```

### Run Client
```bash
cd iAdmin.Client
dotnet run
```

### Database Migrations
```bash
cd iAdmin.Data
dotnet ef migrations add [MigrationName]
dotnet ef database update
```

## Configuration

### appsettings.json (Server)
```json
{
  "Jwt": {
    "SecretKey": "...",
    "ExpiryMinutes": 15,
    "RefreshExpiryDays": 7
  },
  "Database": {
    "ConnectionString": "..."
  }
}
```

### App.config (Client)
```xml
<configuration>
  <appSettings>
    <add key="ServerUrl" value="https://localhost:5001" />
    <add key="UpdateCheckIntervalMinutes" value="60" />
  </appSettings>
</configuration>
```

## Logging

### Serilog Configuration
- **Client**: File sink to `%APPDATA%/iAdmin/logs/`
- **Server**: File + Console sinks
- **Format**: JSON structured logging
- **Level**: Information (configurable to Debug for troubleshooting)

## Deployment & Installation

### Server
- Hosted on Windows Server or Linux with .NET 10 runtime
- HTTPS required
- Database: SQLite or SQL Server

### Client
- Windows 10+ with .NET 10 runtime
- Installed via MSI or ClickOnce deployment
- Auto-update enabled by default
- Requires administrator account (but not active elevation until requested)

## Future Enhancements
- OAuth/SAML integration
- Multi-factor authentication (TOTP)
- Remote deployment of updates
- Batch operation scheduling
- PowerShell script execution integration
