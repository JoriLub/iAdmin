using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace iAdmin.Server.Services;

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresInMinutes { get; set; }
}

public interface ITokenService
{
    TokenResponse GenerateTokens(Guid userId, string username);
    TokenValidationResult ValidateAccessToken(string token);
    TokenValidationResult ValidateRefreshToken(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public TokenResponse GenerateTokens(Guid userId, string username)
    {
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT secret key not configured");
            
            var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var expiry)
                ? expiry
                : 15;

            var accessToken = GenerateAccessToken(userId.ToString(), username, secretKey, expiryMinutes);
            var refreshToken = GenerateRefreshToken(userId.ToString(), username, secretKey);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresInMinutes = expiryMinutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tokens for user {UserId}", userId);
            throw;
        }
    }

    public TokenValidationResult ValidateAccessToken(string token)
    {
        return ValidateToken(token, isRefreshToken: false);
    }

    public TokenValidationResult ValidateRefreshToken(string token)
    {
        return ValidateToken(token, isRefreshToken: true);
    }

    private string GenerateAccessToken(string userId, string username, string secretKey, int expiryMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("type", "access")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "iAdmin",
            audience: _configuration["Jwt:Audience"] ?? "iAdmin-client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(string userId, string username, string secretKey)
    {
        var refreshExpiryDays = int.TryParse(_configuration["Jwt:RefreshExpiryDays"], out var days)
            ? days
            : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("type", "refresh")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "iAdmin",
            audience: _configuration["Jwt:Audience"] ?? "iAdmin-client",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(refreshExpiryDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationResult ValidateToken(string token, bool isRefreshToken)
    {
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT secret key not configured");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token format"
                };
            }

            var tokenType = jwtToken.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
            var expectedType = isRefreshToken ? "refresh" : "access";

            if (tokenType != expectedType)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Expected {expectedType} token, got {tokenType}"
                };
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new TokenValidationResult
            {
                IsValid = true,
                UserId = userId
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token signature"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }
}
