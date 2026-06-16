namespace iAdmin.Common.Enums;

public enum AdminSessionStatus
{
    Active = 1,
    Expired = 2,
    Closed = 3,
    Revoked = 4
}

public enum UpdateStatus
{
    Available = 1,
    Downloading = 2,
    Downloaded = 3,
    Installing = 4,
    Installed = 5,
    Failed = 6,
    Cancelled = 7
}

public enum AuditActionType
{
    SoftwareUninstall = 1,
    SystemConfigChange = 2,
    ServiceManagement = 3,
    SecurityPolicyChange = 4,
    UserManagement = 5,
    AdminSessionCreated = 6,
    AdminSessionClosed = 7,
    UpdateApplied = 8,
    LoginAttempt = 9
}
