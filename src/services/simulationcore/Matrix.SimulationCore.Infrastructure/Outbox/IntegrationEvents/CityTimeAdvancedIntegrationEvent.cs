namespace Matrix.SimulationCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record CityTimeAdvancedIntegrationEvent(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId,
        decimal SpeedMultiplier,
        DateTime OccurredOnUtc);
}
