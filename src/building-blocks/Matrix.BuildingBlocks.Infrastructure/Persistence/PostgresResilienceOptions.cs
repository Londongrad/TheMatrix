namespace Matrix.BuildingBlocks.Infrastructure.Persistence
{
    public sealed class PostgresResilienceOptions
    {
        public const string SectionName = "PostgresResilience";

        public int MaxRetryCount { get; init; } = 5;

        public int MaxRetryDelaySeconds { get; init; } = 10;
    }
}
