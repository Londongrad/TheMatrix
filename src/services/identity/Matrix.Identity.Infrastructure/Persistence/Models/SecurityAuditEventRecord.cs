using Matrix.Identity.Application.Abstractions.Services.Security;

namespace Matrix.Identity.Infrastructure.Persistence.Models
{
    public sealed class SecurityAuditEventRecord
    {
        private SecurityAuditEventRecord() { }
        public Guid Id { get; private set; }
        public SecurityAuditEventType EventType { get; private set; }
        public bool IsSuccessful { get; private set; }
        public Guid? UserId { get; private set; }
        public Guid? SessionId { get; private set; }
        public string? Subject { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public string? DeviceId { get; private set; }
        public string? DeviceName { get; private set; }
        public string? Details { get; private set; }
        public DateTime OccurredAtUtc { get; private set; }

        public static SecurityAuditEventRecord Create(
            SecurityAuditEntry entry,
            DateTime occurredAtUtc)
        {
            return new SecurityAuditEventRecord
            {
                Id = Guid.NewGuid(),
                EventType = entry.EventType,
                IsSuccessful = entry.IsSuccessful,
                UserId = entry.UserId,
                SessionId = entry.SessionId,
                Subject = Normalize(
                    value: entry.Subject,
                    maxLength: 256),
                IpAddress = Normalize(
                    value: entry.IpAddress,
                    maxLength: 64),
                UserAgent = Normalize(
                    value: entry.UserAgent,
                    maxLength: 512),
                DeviceId = Normalize(
                    value: entry.DeviceId,
                    maxLength: 128),
                DeviceName = Normalize(
                    value: entry.DeviceName,
                    maxLength: 256),
                Details = Normalize(
                    value: entry.Details,
                    maxLength: 512),
                OccurredAtUtc = occurredAtUtc
            };
        }

        private static string? Normalize(
            string? value,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed[..maxLength];
        }
    }
}
