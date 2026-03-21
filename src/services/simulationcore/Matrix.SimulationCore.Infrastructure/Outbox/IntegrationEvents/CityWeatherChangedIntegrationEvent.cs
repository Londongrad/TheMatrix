namespace Matrix.SimulationCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record CityWeatherChangedIntegrationEvent(
        Guid CityId,
        WeatherStateIntegrationData PreviousState,
        WeatherStateIntegrationData CurrentState,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
