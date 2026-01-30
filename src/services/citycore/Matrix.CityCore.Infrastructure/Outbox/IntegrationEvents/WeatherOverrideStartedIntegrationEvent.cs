namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record WeatherOverrideStartedIntegrationEvent(
        Guid CityId,
        WeatherStateIntegrationData ForcedState,
        string Source,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        string? Reason,
        DateTime OccurredOnUtc);
}
