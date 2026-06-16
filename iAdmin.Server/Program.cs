using iAdmin.Data.Context;
using iAdmin.Data.Repositories;
using iAdmin.Server.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "iAdmin", "Logs");
Directory.CreateDirectory(appDataPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(appDataPath, "server-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Database
var dataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "iAdmin");
Directory.CreateDirectory(dataPath);

var connectionString = $"Data Source={Path.Combine(dataPath, "iAdmin.db")}";
builder.Services.AddDbContext<IAdminDbContext>(options =>
    options.UseSqlite(connectionString));

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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowClient");

app.MapControllers();

app.Run();

