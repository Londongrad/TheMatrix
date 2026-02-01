namespace Matrix.CityCore.Contracts.Events
{
    public sealed record ClimateProfileChangedV1(
        Guid CityId,
        WeatherClimateProfileV1 PreviousProfile,
        WeatherClimateProfileV1 CurrentProfile,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
