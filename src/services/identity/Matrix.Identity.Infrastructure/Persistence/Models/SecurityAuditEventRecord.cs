using Matrix.Identity.Application.Abstractions.Services.Security;

namespace Matrix.Identity.Infrastructure.Persistence.Models
{
    public sealed class SecurityAuditEventRecord
    {
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

        private SecurityAuditEventRecord() { }

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
                Subject = Normalize(entry.Subject, 256),
                IpAddress = Normalize(entry.IpAddress, 64),
                UserAgent = Normalize(entry.UserAgent, 512),
                DeviceId = Normalize(entry.DeviceId, 128),
                DeviceName = Normalize(entry.DeviceName, 256),
                Details = Normalize(entry.Details, 512),
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
