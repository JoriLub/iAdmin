using iAdmin.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;

namespace iAdmin.Client.Services;

public interface IInstalledApplicationService
{
    Task<IReadOnlyList<InstalledApplication>> GetInstalledApplicationsAsync();
    Task<UninstallExecutionResult> UninstallApplicationAsync(InstalledApplication application);
}

public class InstalledApplicationService : IInstalledApplicationService
{
    private static readonly string[] UninstallRegistryPaths =
    [
        @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
        @"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
    ];

    private readonly ILogger<InstalledApplicationService> _logger;

    public InstalledApplicationService(ILogger<InstalledApplicationService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<InstalledApplication>> GetInstalledApplicationsAsync()
    {
        return Task.Run<IReadOnlyList<InstalledApplication>>(() =>
        {
            var apps = new Dictionary<string, InstalledApplication>(StringComparer.OrdinalIgnoreCase);

            foreach (var app in ReadInstalledApplications(RegistryHive.LocalMachine))
            {
                apps.TryAdd(app.DisplayName, app);
            }

            foreach (var app in ReadInstalledApplications(RegistryHive.CurrentUser))
            {
                apps.TryAdd(app.DisplayName, app);
            }

            return apps.Values
                .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });
    }

    public async Task<UninstallExecutionResult> UninstallApplicationAsync(InstalledApplication application)
    {
        var uninstallCommand = string.IsNullOrWhiteSpace(application.QuietUninstallString)
            ? application.UninstallString
            : application.QuietUninstallString;

        if (string.IsNullOrWhiteSpace(uninstallCommand))
        {
            _logger.LogWarning("No uninstall command available for {DisplayName}", application.DisplayName);
            return new UninstallExecutionResult
            {
                ApplicationName = application.DisplayName,
                Command = "",
                Success = false,
                Message = "Geen uninstall-commando beschikbaar voor deze applicatie.",
                ExitCode = null
            };
        }

        try
        {
            var commandToExecute = EnsureSilentUninstall(uninstallCommand);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {commandToExecute}",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start uninstall process for {DisplayName}", application.DisplayName);
                return new UninstallExecutionResult
                {
                    ApplicationName = application.DisplayName,
                    Command = commandToExecute,
                    Success = false,
                    Message = "Uninstall-proces kon niet gestart worden.",
                    ExitCode = null
                };
            }

            await process.WaitForExitAsync();

            var success = process.ExitCode is 0 or 3010;
            if (!success)
            {
                _logger.LogWarning("Uninstall process failed for {DisplayName}. ExitCode: {ExitCode}",
                    application.DisplayName,
                    process.ExitCode);
            }

            return new UninstallExecutionResult
            {
                ApplicationName = application.DisplayName,
                Command = commandToExecute,
                Success = success,
                Message = success
                    ? "Uninstall is gestart of succesvol afgerond."
                    : "Uninstall is mislukt.",
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uninstalling application {DisplayName}", application.DisplayName);
            return new UninstallExecutionResult
            {
                ApplicationName = application.DisplayName,
                Command = uninstallCommand,
                Success = false,
                Message = $"Fout tijdens uninstall: {ex.Message}",
                ExitCode = null
            };
        }
    }

    private IEnumerable<InstalledApplication> ReadInstalledApplications(RegistryHive hive)
    {
        foreach (var path in UninstallRegistryPaths)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var uninstallKey = baseKey.OpenSubKey(path);
            if (uninstallKey == null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var appKey = uninstallKey.OpenSubKey(subKeyName);
                if (appKey == null)
                {
                    continue;
                }

                var displayName = appKey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var systemComponent = appKey.GetValue("SystemComponent") as int? ?? 0;
                if (systemComponent == 1)
                {
                    continue;
                }

                var uninstallString = appKey.GetValue("UninstallString") as string;
                var quietUninstallString = appKey.GetValue("QuietUninstallString") as string;

                if (string.IsNullOrWhiteSpace(uninstallString) && string.IsNullOrWhiteSpace(quietUninstallString))
                {
                    continue;
                }

                yield return new InstalledApplication
                {
                    DisplayName = displayName.Trim(),
                    DisplayVersion = (appKey.GetValue("DisplayVersion") as string)?.Trim(),
                    Publisher = (appKey.GetValue("Publisher") as string)?.Trim(),
                    InstallLocation = (appKey.GetValue("InstallLocation") as string)?.Trim(),
                    UninstallString = uninstallString,
                    QuietUninstallString = quietUninstallString,
                    RegistryPath = $"{hive}\\{path}\\{subKeyName}"
                };
            }
        }
    }

    private static string EnsureSilentUninstall(string command)
    {
        var normalized = command.Trim();
        if (normalized.StartsWith("MsiExec", StringComparison.OrdinalIgnoreCase))
        {
            var hasQuietSwitch = normalized.Contains("/qn", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/quiet", StringComparison.OrdinalIgnoreCase);

            if (!hasQuietSwitch)
            {
                normalized += " /qn /norestart";
            }
        }

        return normalized;
    }
}
