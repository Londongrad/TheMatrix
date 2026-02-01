namespace Matrix.CityCore.Contracts.Events
{
    public sealed record WeatherOverrideExpiredV1(
        Guid CityId,
        WeatherStateV1 ForcedState,
        string Source,
        DateTimeOffset ExpiredAtUtc,
        DateTime OccurredOnUtc);
}
