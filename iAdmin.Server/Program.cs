using iAdmin.Data.Context;
using iAdmin.Data.Repositories;
using iAdmin.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration (supports appsettings.Production.json overrides)
var configuredLogPath = builder.Configuration["Serilog:LogPath"]
    ?? Path.Combine("logs", "api-.log");

var logPath = Path.IsPathRooted(configuredLogPath)
    ? configuredLogPath
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredLogPath));

var logDirectory = Path.GetDirectoryName(logPath);
if (!string.IsNullOrWhiteSpace(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://localhost:5001", "http://localhost:5000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Database
var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["Database:ConnectionString"]
    ?? "Data Source=iAdmin.db";

var connectionString = NormalizeSqliteConnectionString(configuredConnectionString, AppContext.BaseDirectory);
EnsureSqliteDataSourceDirectoryExists(connectionString, AppContext.BaseDirectory);

Log.Information("API startup config. Environment: {Environment}; LogPath: {LogPath}; ConnectionString: {ConnectionString}",
    builder.Environment.EnvironmentName,
    logPath,
    connectionString);

builder.Services.AddDbContext<IAdminDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Repository pattern
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminSessionRepository, AdminSessionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUpdateHistoryRepository, UpdateHistoryRepository>();

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IDataSeederService, DataSeederService>();

// JWT Configuration
var jwtSecret = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT:SecretKey not configured");
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        var key = System.Text.Encoding.ASCII.GetBytes(jwtSecret);
        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IAdminDbContext>();
    dbContext.Database.Migrate();
    
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeederService>();
    await seeder.SeedInitialDataAsync();
    
    Log.Information("Database initialized and seeded");
}

// Configure middleware
var enableApiDocs = builder.Configuration.GetValue<bool?>("ApiDocs:Enabled")
    ?? app.Environment.IsDevelopment();

if (enableApiDocs)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "iAdmin API";
    });
}

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowClient");

app.MapGet("/health/ping", (HttpContext httpContext) =>
{
    Log.Information("Health ping received from {RemoteIp} with host {Host}",
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        httpContext.Request.Host.Value);

    return Results.Ok(new
    {
        status = "ok",
        utc = DateTime.UtcNow,
        host = httpContext.Request.Host.Value,
        env = app.Environment.EnvironmentName
    });
}).AllowAnonymous();

app.MapControllers();

app.Run();

static string NormalizeSqliteConnectionString(string connectionString, string basePath)
{
    var parts = connectionString.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    for (var i = 0; i < parts.Length; i++)
    {
        if (!parts[i].StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var dataSource = parts[i]["Data Source=".Length..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:" || Path.IsPathRooted(dataSource))
        {
            return string.Join(';', parts);
        }

        var absolutePath = Path.GetFullPath(Path.Combine(basePath, dataSource));
        parts[i] = $"Data Source={absolutePath}";
        return string.Join(';', parts);
    }

    return connectionString;
}

static void EnsureSqliteDataSourceDirectoryExists(string connectionString, string basePath)
{
    var parts = connectionString.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    foreach (var part in parts)
    {
        if (!part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var dataSource = part["Data Source=".Length..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
        {
            return;
        }

        var absolutePath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.GetFullPath(Path.Combine(basePath, dataSource));

        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return;
    }
}

