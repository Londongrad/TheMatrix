namespace Matrix.Identity.Infrastructure.Security.Tokens.Cleanup
{
    public sealed class RefreshTokenCleanupOptions
    {
        public const string SectionName = "RefreshTokenCleanup";

        public bool CleanupEnabled { get; init; } = true;
        public int PollIntervalSeconds { get; init; } = 300;
        public int BatchSize { get; init; } = 500;
        public int RevokedRetentionHours { get; init; } = 24;
        public int ExpiredRetentionHours { get; init; } = 24;
    }
}
