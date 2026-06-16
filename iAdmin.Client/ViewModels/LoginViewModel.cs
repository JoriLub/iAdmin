using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iAdmin.Client.Services;
using Microsoft.Extensions.Logging;

namespace iAdmin.Client.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isLoginSuccessful = false;

    [ObservableProperty]
    private bool isAuthenticated = false;

    [ObservableProperty]
    private bool autoLoginEnabled = false;

    public LoginViewModel(
        IApiClient apiClient,
        ITokenService tokenService,
        ILogger<LoginViewModel> logger)
    {
        _apiClient = apiClient;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            AutoLoginEnabled = await _tokenService.GetAutoLoginPreferenceAsync();

            if (!AutoLoginEnabled)
            {
                return;
            }

            IsLoading = true;
            var tokens = await _tokenService.GetTokensAsync();
            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                return;
            }

            var refreshResult = await _apiClient.RefreshTokenAsync(tokens.RefreshToken);
            if (refreshResult == null)
            {
                await _tokenService.ClearTokensAsync();
                ErrorMessage = "Automatisch inloggen is mislukt. Log opnieuw in.";
                return;
            }

            await _tokenService.SaveTokensAsync(refreshResult.AccessToken, refreshResult.RefreshToken);
            _apiClient.SetAuthToken(refreshResult.AccessToken);
            IsLoginSuccessful = true;
            IsAuthenticated = true;
            ErrorMessage = null;
            _logger.LogInformation("Auto-login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-login error");
            ErrorMessage = "Automatisch inloggen is mislukt.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            ErrorMessage = null;
            IsLoading = true;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vul gebruikersnaam en wachtwoord in.";
                return;
            }

            var response = await _apiClient.LoginAsync(Username, Password);

            if (response != null)
            {
                await _tokenService.SaveTokensAsync(response.AccessToken, response.RefreshToken);
                await _tokenService.SaveAutoLoginPreferenceAsync(AutoLoginEnabled);
                _apiClient.SetAuthToken(response.AccessToken);

                _logger.LogInformation("User logged in successfully. UserId: {UserId}", response.UserId);
                IsLoginSuccessful = true;
                IsAuthenticated = true;
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Ongeldige gebruikersnaam of wachtwoord.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            ErrorMessage = "Er is een fout opgetreden tijdens het inloggen.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void Clear()
    {
        Username = string.Empty;
        Password = string.Empty;
        ErrorMessage = null;
    }
}
