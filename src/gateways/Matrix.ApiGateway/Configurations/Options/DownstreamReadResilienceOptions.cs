namespace Matrix.ApiGateway.Configurations.Options
{
    public sealed class DownstreamReadResilienceOptions
    {
        public const string SectionName = "DownstreamReadResilience";

        public bool Enabled { get; init; } = true;

        public int MaxRetryAttempts { get; init; } = 2;

        public int BaseRetryDelayMilliseconds { get; init; } = 200;

        public int CircuitBreakerConsecutiveFailureThreshold { get; init; } = 5;

        public int CircuitBreakDurationSeconds { get; init; } = 30;
    }
}
