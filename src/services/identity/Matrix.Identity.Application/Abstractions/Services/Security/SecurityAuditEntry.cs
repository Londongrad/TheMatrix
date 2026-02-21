namespace Matrix.Identity.Application.Abstractions.Services.Security
{
    public sealed record SecurityAuditEntry(
        SecurityAuditEventType EventType,
        bool IsSuccessful,
        Guid? UserId = null,
        Guid? SessionId = null,
        string? Subject = null,
        string? IpAddress = null,
        string? UserAgent = null,
        string? DeviceId = null,
        string? DeviceName = null,
        string? Details = null);
}
