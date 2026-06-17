namespace iAdmin.Client.Models;

public class UninstallExecutionResult
{
    public required string ApplicationName { get; init; }
    public required string Command { get; init; }
    public required string Message { get; init; }
    public bool Success { get; init; }
    public int? ExitCode { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
