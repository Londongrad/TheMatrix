namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record ClimateProfileChangedIntegrationEvent(
        Guid CityId,
        WeatherClimateProfileIntegrationData PreviousProfile,
        WeatherClimateProfileIntegrationData CurrentProfile,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
