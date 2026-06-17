using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iAdmin.Client.Models;
using iAdmin.Client.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace iAdmin.Client.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ITokenService _tokenService;
    private readonly IInstalledApplicationService _installedApplicationService;
    private readonly IFileSystemAdminService _fileSystemAdminService;
    private readonly ILogger<LoginViewModel> _logger;
    private readonly List<InstalledApplication> _allInstalledApplications = [];

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

    [ObservableProperty]
    private bool isApplicationsLoading = false;

    [ObservableProperty]
    private string? applicationsStatusMessage;

    [ObservableProperty]
    private string applicationSearchText = string.Empty;

    [ObservableProperty]
    private InstalledApplication? selectedApplication;

    [ObservableProperty]
    private bool isUninstallInProgress = false;

    [ObservableProperty]
    private string uninstallDetails = "Nog geen uninstall uitgevoerd.";

    [ObservableProperty]
    private bool isFileSystemLoading = false;

    [ObservableProperty]
    private string? fileSystemStatusMessage;

    [ObservableProperty]
    private string? selectedDrive;

    [ObservableProperty]
    private string currentBrowsePath = string.Empty;

    [ObservableProperty]
    private FileSystemEntry? selectedFileSystemEntry;

    [ObservableProperty]
    private string copyMoveSourcePath = string.Empty;

    [ObservableProperty]
    private string copyMoveDestinationPath = string.Empty;

    [ObservableProperty]
    private bool overwriteExisting = true;

    public ObservableCollection<InstalledApplication> InstalledApplications { get; } = [];
    public ObservableCollection<UninstallExecutionResult> UninstallHistory { get; } = [];
    public ObservableCollection<string> AvailableDrives { get; } = [];
    public ObservableCollection<FileSystemEntry> FileSystemEntries { get; } = [];

    public LoginViewModel(
        IApiClient apiClient,
        ITokenService tokenService,
        IInstalledApplicationService installedApplicationService,
        IFileSystemAdminService fileSystemAdminService,
        ILogger<LoginViewModel> logger)
    {
        _apiClient = apiClient;
        _tokenService = tokenService;
        _installedApplicationService = installedApplicationService;
        _fileSystemAdminService = fileSystemAdminService;
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
            await LoadInstalledApplicationsAsync();
            await LoadDrivesAsync();
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
                await LoadInstalledApplicationsAsync();
                await LoadDrivesAsync();
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

    partial void OnApplicationSearchTextChanged(string value)
    {
        ApplySearchFilter();
    }

    [RelayCommand]
    public async Task LoadInstalledApplicationsAsync()
    {
        try
        {
            IsApplicationsLoading = true;
            ApplicationsStatusMessage = "Geinstalleerde applicaties worden geladen...";

            var applications = await _installedApplicationService.GetInstalledApplicationsAsync();

            _allInstalledApplications.Clear();
            _allInstalledApplications.AddRange(applications);
            ApplySearchFilter();

            ApplicationsStatusMessage = $"{InstalledApplications.Count} applicaties gevonden.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading installed applications");
            ApplicationsStatusMessage = "Fout bij laden van applicaties.";
        }
        finally
        {
            IsApplicationsLoading = false;
            UninstallSelectedApplicationCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    public async Task LoadDrivesAsync()
    {
        try
        {
            IsFileSystemLoading = true;
            FileSystemStatusMessage = "Schijfstations laden...";

            var drives = await _fileSystemAdminService.GetAvailableDrivesAsync();
            AvailableDrives.Clear();
            foreach (var drive in drives)
            {
                AvailableDrives.Add(drive);
            }

            if (AvailableDrives.Count > 0)
            {
                SelectedDrive = AvailableDrives[0];
                CurrentBrowsePath = SelectedDrive;
                await LoadFileSystemEntriesAsync();
            }
            else
            {
                FileSystemEntries.Clear();
                FileSystemStatusMessage = "Geen schijfstations gevonden.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading drives");
            FileSystemStatusMessage = "Fout bij laden van schijfstations.";
        }
        finally
        {
            IsFileSystemLoading = false;
            DeleteSelectedFileSystemEntryCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    public async Task LoadFileSystemEntriesAsync()
    {
        try
        {
            IsFileSystemLoading = true;

            if (string.IsNullOrWhiteSpace(CurrentBrowsePath))
            {
                if (!string.IsNullOrWhiteSpace(SelectedDrive))
                {
                    CurrentBrowsePath = SelectedDrive;
                }
                else
                {
                    FileSystemStatusMessage = "Geen pad geselecteerd.";
                    return;
                }
            }

            var entries = await _fileSystemAdminService.GetEntriesAsync(CurrentBrowsePath);
            FileSystemEntries.Clear();
            foreach (var entry in entries)
            {
                FileSystemEntries.Add(entry);
            }

            FileSystemStatusMessage = $"{FileSystemEntries.Count} items gevonden in {CurrentBrowsePath}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file system entries for {Path}", CurrentBrowsePath);
            FileSystemStatusMessage = "Fout bij laden van bestanden/mappen.";
        }
        finally
        {
            IsFileSystemLoading = false;
            DeleteSelectedFileSystemEntryCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedFileSystemEntry))]
    public async Task DeleteSelectedFileSystemEntryAsync()
    {
        if (SelectedFileSystemEntry == null)
        {
            return;
        }

        var confirmationResult = MessageBox.Show(
            $"Permanent verwijderen van '{SelectedFileSystemEntry.FullPath}'? Dit kan niet ongedaan worden gemaakt.",
            "Permanent verwijderen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmationResult != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsFileSystemLoading = true;
            var success = await _fileSystemAdminService.DeletePermanentlyAsync(SelectedFileSystemEntry.FullPath);
            FileSystemStatusMessage = success
                ? "Item permanent verwijderd."
                : "Permanent verwijderen is mislukt.";

            if (success)
            {
                await LoadFileSystemEntriesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file system entry {Path}", SelectedFileSystemEntry.FullPath);
            FileSystemStatusMessage = "Fout tijdens permanent verwijderen.";
        }
        finally
        {
            IsFileSystemLoading = false;
            DeleteSelectedFileSystemEntryCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    public async Task CopyPathAsync()
    {
        await ExecuteCopyMoveAsync(isMove: false);
    }

    [RelayCommand]
    public async Task MovePathAsync()
    {
        await ExecuteCopyMoveAsync(isMove: true);
    }

    partial void OnSelectedDriveChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            CurrentBrowsePath = value;
            _ = LoadFileSystemEntriesAsync();
        }
    }

    partial void OnSelectedFileSystemEntryChanged(FileSystemEntry? value)
    {
        DeleteSelectedFileSystemEntryCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsFileSystemLoadingChanged(bool value)
    {
        DeleteSelectedFileSystemEntryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUninstallSelectedApplication))]
    public async Task UninstallSelectedApplicationAsync()
    {
        if (SelectedApplication == null)
        {
            return;
        }

        var applicationName = SelectedApplication.DisplayName;

        var confirmationResult = MessageBox.Show(
            $"Weet je zeker dat je '{applicationName}' wilt verwijderen?",
            "Applicatie verwijderen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmationResult != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsUninstallInProgress = true;
            IsApplicationsLoading = true;
            ApplicationsStatusMessage = $"Verwijderen van '{applicationName}' gestart...";

            var result = await _installedApplicationService.UninstallApplicationAsync(SelectedApplication);

            UninstallHistory.Insert(0, result);
            UninstallDetails = $"Naam: {result.ApplicationName}\n" +
                               $"Status: {(result.Success ? "Succes" : "Mislukt")}\n" +
                               $"ExitCode: {(result.ExitCode.HasValue ? result.ExitCode.Value.ToString() : "n.v.t.")}\n" +
                               $"Commando: {result.Command}\n" +
                               $"Bericht: {result.Message}";

            ApplicationsStatusMessage = result.Success
                ? $"'{applicationName}' is verwijderd of verwijdering is gestart."
                : $"Verwijderen van '{applicationName}' is mislukt.";

            if (result.Success)
            {
                await LoadInstalledApplicationsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uninstalling selected application");
            ApplicationsStatusMessage = "Fout tijdens verwijderen van applicatie.";
        }
        finally
        {
            IsApplicationsLoading = false;
            IsUninstallInProgress = false;
            UninstallSelectedApplicationCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanUninstallSelectedApplication()
    {
        return SelectedApplication != null && !IsApplicationsLoading;
    }

    private bool CanDeleteSelectedFileSystemEntry()
    {
        return SelectedFileSystemEntry != null && !IsFileSystemLoading;
    }

    partial void OnSelectedApplicationChanged(InstalledApplication? value)
    {
        UninstallSelectedApplicationCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsApplicationsLoadingChanged(bool value)
    {
        UninstallSelectedApplicationCommand.NotifyCanExecuteChanged();
    }

    private void ApplySearchFilter()
    {
        var query = ApplicationSearchText?.Trim();
        IEnumerable<InstalledApplication> filtered = _allInstalledApplications;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(app =>
                app.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(app.Publisher) && app.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(app.DisplayVersion) && app.DisplayVersion.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        InstalledApplications.Clear();
        foreach (var app in filtered)
        {
            InstalledApplications.Add(app);
        }
    }

    private async Task ExecuteCopyMoveAsync(bool isMove)
    {
        if (string.IsNullOrWhiteSpace(CopyMoveSourcePath) || string.IsNullOrWhiteSpace(CopyMoveDestinationPath))
        {
            FileSystemStatusMessage = "Bronpad en doelpad zijn verplicht.";
            return;
        }

        try
        {
            IsFileSystemLoading = true;
            var success = isMove
                ? await _fileSystemAdminService.MoveAsync(CopyMoveSourcePath, CopyMoveDestinationPath, OverwriteExisting)
                : await _fileSystemAdminService.CopyAsync(CopyMoveSourcePath, CopyMoveDestinationPath, OverwriteExisting);

            FileSystemStatusMessage = success
                ? (isMove ? "Verplaatsen voltooid." : "Kopieren voltooid.")
                : (isMove ? "Verplaatsen mislukt." : "Kopieren mislukt.");

            if (success && !string.IsNullOrWhiteSpace(CurrentBrowsePath) && Directory.Exists(CurrentBrowsePath))
            {
                await LoadFileSystemEntriesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {Action} operation", isMove ? "move" : "copy");
            FileSystemStatusMessage = isMove ? "Fout tijdens verplaatsen." : "Fout tijdens kopieren.";
        }
        finally
        {
            IsFileSystemLoading = false;
        }
    }
}
