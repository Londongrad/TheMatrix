namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record CityTimeAdvancedIntegrationEvent(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId,
        decimal SpeedMultiplier,
        DateTime OccurredOnUtc);
}
