namespace Matrix.CityCore.Contracts.Events
{
    public sealed record CityWeatherCreatedV1(
        Guid CityId,
        WeatherClimateProfileV1 ClimateProfile,
        WeatherStateV1 InitialState,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
