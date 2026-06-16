using iAdmin.Common.Dtos;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;

namespace iAdmin.Client.Services;

/// <summary>
/// Service for API communication
/// </summary>
public interface IApiClient
{
    Task<LoginResponseDto?> LoginAsync(string username, string password);
    Task<RefreshTokenResponseDto?> RefreshTokenAsync(string refreshToken);
    Task<UpdateInfoDto?> GetLatestUpdateAsync();
    void SetAuthToken(string accessToken);
    void ClearAuthToken();
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private string? _authToken;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://localhost:5000");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequestDto { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            }

            _logger.LogWarning("Login failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return null;
        }
    }

    public async Task<RefreshTokenResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new RefreshTokenRequestDto { RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
            }

            _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    public async Task<UpdateInfoDto?> GetLatestUpdateAsync()
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync("/api/updates/latest");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UpdateInfoDto>();
            }

            _logger.LogWarning("Get update info failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest update");
            return null;
        }
    }

    public void SetAuthToken(string accessToken)
    {
        _authToken = accessToken;
    }

    public void ClearAuthToken()
    {
        _authToken = null;
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
    }

    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        }
    }
}
