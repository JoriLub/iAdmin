namespace iAdmin.Client.Models;

public class FileSystemEntry
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required string EntryType { get; init; }
    public long? SizeBytes { get; init; }
    public DateTime? LastModifiedUtc { get; init; }

    public string SizeDisplay => SizeBytes.HasValue ? FormatSize(SizeBytes.Value) : "n.v.t.";

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}
