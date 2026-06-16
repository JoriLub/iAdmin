using iAdmin.Common.Enums;

namespace iAdmin.Common.Models;

public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}

public class AdminSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public required string IpAddress { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public AdminSessionStatus Status { get; set; } = AdminSessionStatus.Active;
    public string? CloseReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User? User { get; set; }
}

public class UpdateHistory
{
    public Guid UpdateId { get; set; } = Guid.NewGuid();
    public required string Version { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public UpdateStatus Status { get; set; }
    public required string ChecksumSha256 { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuditLog
{
    public Guid AuditId { get; set; } = Guid.NewGuid();
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public AuditActionType ActionType { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public required string ChangedBy { get; set; }
    public required string IpAddress { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? FailureReason { get; set; }
}
