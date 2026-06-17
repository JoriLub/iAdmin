using iAdmin.Client.Models;
using Microsoft.Extensions.Logging;
using System.IO;

namespace iAdmin.Client.Services;

public interface IFileSystemAdminService
{
    Task<IReadOnlyList<string>> GetAvailableDrivesAsync();
    Task<IReadOnlyList<FileSystemEntry>> GetEntriesAsync(string path);
    Task<bool> DeletePermanentlyAsync(string path);
    Task<bool> CopyAsync(string sourcePath, string destinationPath, bool overwrite);
    Task<bool> MoveAsync(string sourcePath, string destinationPath, bool overwrite);
}

public class FileSystemAdminService : IFileSystemAdminService
{
    private readonly ILogger<FileSystemAdminService> _logger;

    public FileSystemAdminService(ILogger<FileSystemAdminService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<string>> GetAvailableDrivesAsync()
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.RootDirectory.FullName)
                .OrderBy(d => d)
                .ToList();
        });
    }

    public Task<IReadOnlyList<FileSystemEntry>> GetEntriesAsync(string path)
    {
        return Task.Run<IReadOnlyList<FileSystemEntry>>(() =>
        {
            var result = new List<FileSystemEntry>();

            if (!Directory.Exists(path))
            {
                return result;
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                try
                {
                    var info = new DirectoryInfo(directory);
                    result.Add(new FileSystemEntry
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        EntryType = "Map",
                        SizeBytes = TryGetDirectorySize(info),
                        LastModifiedUtc = info.LastWriteTimeUtc
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to read directory {Directory}", directory);
                }
            }

            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    var info = new FileInfo(file);
                    result.Add(new FileSystemEntry
                    {
                        Name = info.Name,
                        FullPath = info.FullName,
                        EntryType = "Bestand",
                        SizeBytes = info.Length,
                        LastModifiedUtc = info.LastWriteTimeUtc
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to read file {File}", file);
                }
            }

            return result
                .OrderBy(r => r.EntryType)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });
    }

    public async Task<bool> DeletePermanentlyAsync(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }

            if (Directory.Exists(path))
            {
                await Task.Run(() => Directory.Delete(path, recursive: true));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permanent delete failed for {Path}", path);
            return false;
        }
    }

    public async Task<bool> CopyAsync(string sourcePath, string destinationPath, bool overwrite)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite));
                return true;
            }

            if (Directory.Exists(sourcePath))
            {
                await Task.Run(() => CopyDirectory(sourcePath, destinationPath, overwrite));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed. Source: {Source}, Destination: {Destination}", sourcePath, destinationPath);
            return false;
        }
    }

    public async Task<bool> MoveAsync(string sourcePath, string destinationPath, bool overwrite)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                await Task.Run(() =>
                {
                    if (overwrite && File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                    File.Move(sourcePath, destinationPath);
                });
                return true;
            }

            if (Directory.Exists(sourcePath))
            {
                await Task.Run(() =>
                {
                    if (overwrite && Directory.Exists(destinationPath))
                    {
                        Directory.Delete(destinationPath, true);
                    }
                    Directory.Move(sourcePath, destinationPath);
                });
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Move failed. Source: {Source}, Destination: {Destination}", sourcePath, destinationPath);
            return false;
        }
    }

    private static long? TryGetDirectorySize(DirectoryInfo directory)
    {
        try
        {
            return GetDirectorySizeRecursive(directory);
        }
        catch
        {
            return null;
        }
    }

    private static long GetDirectorySizeRecursive(DirectoryInfo directory)
    {
        long total = 0;

        foreach (var file in directory.GetFiles())
        {
            total += file.Length;
        }

        foreach (var childDirectory in directory.GetDirectories())
        {
            total += GetDirectorySizeRecursive(childDirectory);
        }

        return total;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite)
    {
        var source = new DirectoryInfo(sourceDir);
        if (!source.Exists)
        {
            throw new DirectoryNotFoundException($"Bronmap niet gevonden: {sourceDir}");
        }

        Directory.CreateDirectory(destinationDir);

        foreach (var file in source.GetFiles())
        {
            var destinationFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(destinationFilePath, overwrite);
        }

        foreach (var directory in source.GetDirectories())
        {
            var destinationChildPath = Path.Combine(destinationDir, directory.Name);
            CopyDirectory(directory.FullName, destinationChildPath, overwrite);
        }
    }
}
