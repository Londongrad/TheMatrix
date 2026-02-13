namespace Matrix.Population.Infrastructure.Messaging.Cleanup
{
    public sealed class ProcessedIntegrationMessageCleanupOptions
    {
        public const string SectionName = "ProcessedIntegrationMessageCleanup";

        public bool CleanupEnabled { get; init; } = true;
        public int PollIntervalSeconds { get; init; } = 300;
        public int BatchSize { get; init; } = 1000;
        public int RetentionHours { get; init; } = 24;
    }
}
