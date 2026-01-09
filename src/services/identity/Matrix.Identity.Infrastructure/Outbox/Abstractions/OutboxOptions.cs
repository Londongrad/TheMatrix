namespace Matrix.Identity.Infrastructure.Outbox.Abstractions
{
    public sealed class OutboxOptions
    {
        public int BatchSize { get; init; } = 50;
        public int PollIntervalSeconds { get; init; } = 2;
        public int LeaseTtlSeconds { get; init; } = 30;
        public int FailureBackoffMaxSeconds { get; init; } = 300;
    }
}
