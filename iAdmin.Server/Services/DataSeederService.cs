using iAdmin.Common.Models;
using iAdmin.Data.Repositories;
using iAdmin.Server.Services;

namespace iAdmin.Server.Services;

public interface IDataSeederService
{
    Task SeedInitialDataAsync();
}

public class DataSeederService : IDataSeederService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<DataSeederService> _logger;

    public DataSeederService(
        IUserRepository userRepository,
        IAuthenticationService authService,
        ILogger<DataSeederService> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    public async Task SeedInitialDataAsync()
    {
        try
        {
            // Check if admin user exists
            var adminUser = await _userRepository.GetByUsernameAsync("admin");

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Username = "admin",
                    Email = "admin@iadmin.local",
                    PasswordHash = _authService.HashPassword("admin123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(adminUser);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("Seeded default admin user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seeding");
            throw;
        }
    }
}
