namespace Matrix.CityCore.Contracts.Events
{
    public sealed record CityWeatherChangedV1(
        Guid CityId,
        WeatherStateV1 PreviousState,
        WeatherStateV1 CurrentState,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
