using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace iAdmin.Client.Services;

/// <summary>
/// Manages JWT tokens locally for the client
/// </summary>
public interface ITokenService
{
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task<(string? AccessToken, string? RefreshToken)> GetTokensAsync();
    Task SaveAutoLoginPreferenceAsync(bool enabled);
    Task<bool> GetAutoLoginPreferenceAsync();
    Task ClearTokensAsync();
    bool HasValidToken();
}

internal class ClientSettings
{
    public bool AutoLoginEnabled { get; set; }
}

public class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private string? _cachedAccessToken;
    private string? _cachedRefreshToken;
    private DateTime _tokenExpiry;

    private static string GetAppDataPath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "iAdmin");
        Directory.CreateDirectory(appDataPath);
        return appDataPath;
    }

    public TokenService(ILogger<TokenService> logger)
    {
        _logger = logger;
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            _cachedAccessToken = accessToken;
            _cachedRefreshToken = refreshToken;

            // In production, use Windows DPAPI for encryption
            var appDataPath = GetAppDataPath();

            var tokenFile = Path.Combine(appDataPath, ".tokens");
            var tokenData = $"{accessToken}|{refreshToken}";
            
            await File.WriteAllTextAsync(tokenFile, tokenData);
            _logger.LogInformation("Tokens saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tokens");
            throw;
        }
    }

    public async Task<(string? AccessToken, string? RefreshToken)> GetTokensAsync()
    {
        try
        {
            if (_cachedAccessToken != null && _cachedRefreshToken != null)
            {
                return (_cachedAccessToken, _cachedRefreshToken);
            }

            var appDataPath = GetAppDataPath();
            var tokenFile = Path.Combine(appDataPath, ".tokens");

            if (!File.Exists(tokenFile))
            {
                return (null, null);
            }

            var tokenData = await File.ReadAllTextAsync(tokenFile);
            var parts = tokenData.Split('|');

            if (parts.Length != 2)
            {
                return (null, null);
            }

            _cachedAccessToken = parts[0];
            _cachedRefreshToken = parts[1];

            return (_cachedAccessToken, _cachedRefreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tokens");
            return (null, null);
        }
    }

    public async Task SaveAutoLoginPreferenceAsync(bool enabled)
    {
        try
        {
            var settingsPath = Path.Combine(GetAppDataPath(), ".client-settings.json");
            var settings = new ClientSettings { AutoLoginEnabled = enabled };
            var json = JsonSerializer.Serialize(settings);
            await File.WriteAllTextAsync(settingsPath, json);
            _logger.LogInformation("Auto-login preference saved: {Enabled}", enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving auto-login preference");
        }
    }

    public async Task<bool> GetAutoLoginPreferenceAsync()
    {
        try
        {
            var settingsPath = Path.Combine(GetAppDataPath(), ".client-settings.json");
            if (!File.Exists(settingsPath))
            {
                return false;
            }

            var json = await File.ReadAllTextAsync(settingsPath);
            var settings = JsonSerializer.Deserialize<ClientSettings>(json);
            return settings?.AutoLoginEnabled ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading auto-login preference");
            return false;
        }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            _cachedAccessToken = null;
            _cachedRefreshToken = null;

            var appDataPath = GetAppDataPath();
            var tokenFile = Path.Combine(appDataPath, ".tokens");

            if (File.Exists(tokenFile))
            {
                File.Delete(tokenFile);
            }

            _logger.LogInformation("Tokens cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tokens");
        }
    }

    public bool HasValidToken()
    {
        return !string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry;
    }
}
