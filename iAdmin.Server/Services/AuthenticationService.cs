using iAdmin.Common.Dtos;
using iAdmin.Common.Models;
using iAdmin.Data.Repositories;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace iAdmin.Server.Services;

public interface IAuthenticationService
{
    Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, string? ipAddress);
    Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, string? ipAddress)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt failed: user not found or inactive. Username: {Username}, IP: {IpAddress}",
                    request.Username, ipAddress ?? "unknown");
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt failed: invalid password. Username: {Username}, IP: {IpAddress}",
                    request.Username, ipAddress ?? "unknown");
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            user.LastLogin = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var tokens = _tokenService.GenerateTokens(user.UserId, user.Username);
            
            _logger.LogInformation("User logged in successfully. UserId: {UserId}, Username: {Username}, IP: {IpAddress}",
                user.UserId, user.Username, ipAddress ?? "unknown");

            return new LoginResponseDto
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresInMinutes = tokens.ExpiresInMinutes,
                UserId = user.UserId
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error during login. Username: {Username}",
                request.Username);
            throw new InvalidOperationException("An error occurred during authentication", ex);
        }
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        try
        {
            var validationResult = _tokenService.ValidateRefreshToken(refreshToken);
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Refresh token validation failed. IP: {IpAddress}, Reason: {Reason}",
                    ipAddress ?? "unknown", validationResult.ErrorMessage);
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            if (!Guid.TryParse(validationResult.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token used by inactive user. UserId: {UserId}",
                    userId);
                throw new UnauthorizedAccessException("User is not active");
            }

            var tokens = _tokenService.GenerateTokens(user.UserId, user.Username);
            
            _logger.LogInformation("Token refreshed successfully. UserId: {UserId}",
                user.UserId);

            return new RefreshTokenResponseDto
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresInMinutes = tokens.ExpiresInMinutes
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw new InvalidOperationException("An error occurred during token refresh", ex);
        }
    }

    public string HashPassword(string password)
    {
        // BCrypt with cost 12 for secure hashing
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }
}
