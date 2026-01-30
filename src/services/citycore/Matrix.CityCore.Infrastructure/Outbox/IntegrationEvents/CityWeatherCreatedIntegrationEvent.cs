namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record CityWeatherCreatedIntegrationEvent(
        Guid CityId,
        WeatherClimateProfileIntegrationData ClimateProfile,
        WeatherStateIntegrationData InitialState,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
