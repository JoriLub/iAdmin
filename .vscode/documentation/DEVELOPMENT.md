# iAdmin - Development Guide

## Quick Start for Developers

### Initial Setup
1. Clone/open the project in Visual Studio Code
2. Ensure .NET 10 SDK is installed: `dotnet --version`
3. Build the solution: `dotnet build`

### Running the Application

#### Terminal 1: Start Server
```bash
cd iAdmin.Server
dotnet run
```
Server will start on `https://localhost:5001` (HTTP: `http://localhost:5000`)

#### Terminal 2: Start Client
```bash
cd iAdmin.Client
.\Run-ClientAsAdmin.ps1 -Build
```

The client is configured with `requireAdministrator` and will trigger a Windows UAC prompt at startup.
Login API calls use `http://localhost:5000`, so make sure the server is running first.
Na succesvol inloggen schakelt de UI automatisch naar het administrator dashboard.
Als `Automatisch inloggen` is aangevinkt, wordt bij herstart automatisch ingelogd met refresh token.
De client herstelt ook de laatste vensterpositie en -grootte bij opnieuw starten.

If PowerShell blocks script execution, run:
```bash
powershell -ExecutionPolicy Bypass -File .\Run-ClientAsAdmin.ps1 -Build
```

Alternative (manual): start an elevated terminal (Run as Administrator), then run `dotnet run` inside `iAdmin.Client`.

### Default Credentials (Development Only)
- **Username**: `admin`
- **Password**: `admin123`

## Project Structure

```
iAdmin/
├── .vscode/
│   └── documentation/          # Technical documentation
├── iAdmin.Common/              # Shared DTOs, models, enums
├── iAdmin.Data/                # Entity Framework repositories
├── iAdmin.Server/              # ASP.NET Core API
│   ├── Controllers/            # API endpoints
│   ├── Services/               # Business logic
│   └── Properties/
├── iAdmin.Client/              # WPF Desktop Application
│   ├── ViewModels/             # MVVM ViewModels
│   ├── Services/               # Client services
│   ├── Converters/             # WPF value converters
│   └── Resources/              # UI assets
└── README.md                   # Project overview
```

## Development Workflow

### Adding a New Feature

1. **Create DTOs** (if needed)
   ```csharp
   // iAdmin.Common/Dtos/YourDto.cs
   public class YourDto { ... }
   ```

2. **Create API Endpoint**
   ```csharp
   // iAdmin.Server/Controllers/YourController.cs
   [ApiController]
   [Route("api/[controller]")]
   public class YourController : ControllerBase { ... }
   ```

3. **Add Service**
   ```csharp
   // iAdmin.Server/Services/YourService.cs
   public interface IYourService { ... }
   public class YourService : IYourService { ... }
   ```

4. **Register in DI** (Program.cs)
   ```csharp
   builder.Services.AddScoped<IYourService, YourService>();
   ```

5. **Create ViewModel** (if WPF)
   ```csharp
   // iAdmin.Client/ViewModels/YourViewModel.cs
   public partial class YourViewModel : ObservableObject { ... }
   ```

### Database Changes

**Add Migration:**
```bash
cd iAdmin.Server
dotnet ef migrations add YourMigrationName -p ../iAdmin.Data/iAdmin.Data.csproj
```

**Apply Migration:**
```bash
dotnet ef database update -p ../iAdmin.Data/iAdmin.Data.csproj
```

**Revert Migration:**
```bash
dotnet ef migrations remove -p ../iAdmin.Data/iAdmin.Data.csproj
```

## Debugging

### Server Debugging
- Set breakpoints in Visual Studio Code or Visual Studio
- Launch with: `dotnet run` or use VS Code debugger
- Check logs in `%APPDATA%\iAdmin\Logs\server-.log`

### Client Debugging
- Check logs in `%APPDATA%\iAdmin\Logs\client-.log`
- Use XAML designer preview for UI debugging

### Common Issues

**Server won't start:**
- Check if port 5001 is in use: `netstat -ano | findstr :5001`
- Verify JWT secret is configured in appsettings.json

**Client can't connect:**
- Ensure Server is running
- Check firewall settings
- Verify URL in ApiClient matches server address

**Database locked:**
- Close all instances of the app
- Delete SQLite WAL files: `rm %APPDATA%\iAdmin\iAdmin.db-wal`

## Testing

### Manual Test Checklist
- [ ] Login with correct credentials
- [ ] Login fails with wrong password
- [ ] Token refresh works
- [ ] UI displays error messages properly
- [ ] Audit logs are created
- [ ] Application logs are written

### Integration Points
- API responses use correct status codes
- DTOs are properly serialized/deserialized
- Database transactions complete successfully

## Code Style

### Naming Conventions
- Classes: `PascalCase` (e.g., `LoginViewModel`)
- Methods: `PascalCase` (e.g., `LoginAsync()`)
- Properties: `PascalCase` (e.g., `Username`)
- Private fields: `_camelCase` (e.g., `_logger`)
- Constants: `UPPER_CASE` (e.g., `MAX_RETRIES`)

### Method Guidelines
- Async methods end with `Async` suffix
- Use `async/await` for I/O operations
- Log errors with exception: `_logger.LogError(ex, "message")`
- Return nullable types with `?` suffix

### MVVM in WPF
- ViewModels inherit from `ObservableObject` (MVVM Toolkit)
- Properties use `[ObservableProperty]` attribute
- Commands use `[RelayCommand]` attribute
- Views bind to ViewModels via `DataContext`

## Performance Considerations

- Use `.AsNoTracking()` for read-only queries
- Implement pagination for large datasets
- Cache frequently accessed data
- Use connection pooling for database

## Security Guidelines

- Never commit secrets/keys to repository
- Use environment variables for configuration
- Validate all API inputs
- Use HTTPS in production
- Rotate refresh tokens regularly

## Useful Commands

```bash
# Check dependencies
dotnet outdated

# Run project
dotnet run

# Run tests
dotnet test

# Build release
dotnet publish -c Release -o ./publish

# Clean build artifacts
dotnet clean

# Format code
dotnet format

# Analyze code
dotnet analyze
```

## Resources

- [Microsoft .NET Documentation](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/)
- [WPF & XAML](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [MVVM Community Toolkit](https://learn.microsoft.com/windows/communitytoolkit/mvvm/mvvm_introduction)

## Contributing

1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Update documentation
5. Submit for review

## Questions?

Refer to [ARCHITECTURE.md](./ARCHITECTURE.md) for system design details or contact the development team.
