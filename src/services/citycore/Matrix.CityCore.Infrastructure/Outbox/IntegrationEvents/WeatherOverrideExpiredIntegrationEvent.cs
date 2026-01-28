namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record WeatherOverrideExpiredIntegrationEvent(
        Guid CityId,
        WeatherStateIntegrationData ForcedState,
        string Source,
        DateTimeOffset ExpiredAtUtc,
        DateTime OccurredOnUtc);
}