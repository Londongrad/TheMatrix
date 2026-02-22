using Matrix.Identity.Application.Abstractions.Services.Security;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class SecurityActivityItemResult
    {
        public SecurityAuditEventType EventType { get; init; }
        public bool IsSuccessful { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? DeviceId { get; init; }
        public string? DeviceName { get; init; }
        public string? Details { get; init; }
    }
}
