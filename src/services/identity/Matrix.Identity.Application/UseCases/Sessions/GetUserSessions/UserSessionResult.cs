namespace Matrix.Identity.Application.UseCases.Sessions.GetUserSessions
{
    public sealed class UserSessionResult
    {
        public Guid Id { get; init; }

        public string DeviceId { get; init; } = null!;
        public string DeviceName { get; init; } = null!;
        public string UserAgent { get; init; } = null!;
        public string? IpAddress { get; init; }

        public string? Country { get; init; }
        public string? Region { get; init; }
        public string? City { get; init; }

        public DateTime CreatedAtUtc { get; init; }
        public DateTime? LastUsedAtUtc { get; init; }

        public bool IsActive { get; init; }
    }
}
