namespace Matrix.BuildingBlocks.Api.HealthChecks
{
    public sealed class RabbitMqHealthCheckOptions
    {
        public const string SectionName = "HealthChecks:RabbitMq";

        public bool Enabled { get; init; } = true;

        public int TimeoutSeconds { get; init; } = 3;
    }
}
