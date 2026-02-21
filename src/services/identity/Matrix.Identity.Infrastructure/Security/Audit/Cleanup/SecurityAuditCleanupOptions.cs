namespace Matrix.Identity.Infrastructure.Security.Audit.Cleanup
{
    public sealed class SecurityAuditCleanupOptions
    {
        public const string SectionName = "SecurityAuditCleanup";

        public bool CleanupEnabled { get; init; } = true;
        public int PollIntervalSeconds { get; init; } = 3600;
        public int BatchSize { get; init; } = 1000;
        public int RetentionDays { get; init; } = 30;
    }
}
