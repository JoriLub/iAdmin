namespace iAdmin.Common.Dtos;

public class LoginRequestDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class LoginResponseDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresInMinutes { get; set; }
    public required Guid UserId { get; set; }
}

public class RefreshTokenRequestDto
{
    public required string RefreshToken { get; set; }
}

public class RefreshTokenResponseDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresInMinutes { get; set; }
}

public class UpdateInfoDto
{
    public required string Version { get; set; }
    public required string ReleaseNotes { get; set; }
    public required string DownloadUrl { get; set; }
    public required string ChecksumSha256 { get; set; }
    public long SizeBytes { get; set; }
    public DateTime ReleasedAt { get; set; }
    public bool IsMandatory { get; set; }
}

public class AuditLogDto
{
    public Guid AuditId { get; set; }
    public Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required string ActionType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public required string ChangedBy { get; set; }
    public required string IpAddress { get; set; }
}
