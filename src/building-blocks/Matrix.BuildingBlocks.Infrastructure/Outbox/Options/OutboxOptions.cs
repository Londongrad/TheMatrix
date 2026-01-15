namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Options
{
    public sealed class OutboxOptions
    {
        public const string SectionName = "Outbox";

        public int BatchSize { get; init; } = 50;
        public int PollIntervalSeconds { get; init; } = 2;
        public int LeaseTtlSeconds { get; init; } = 30;
        public int FailureBackoffMaxSeconds { get; init; } = 300;
        public bool DispatcherEnabled { get; init; } = true;
    }
}
