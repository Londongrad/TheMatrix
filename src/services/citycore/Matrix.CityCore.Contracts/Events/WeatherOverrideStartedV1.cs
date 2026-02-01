namespace Matrix.CityCore.Contracts.Events
{
    public sealed record WeatherOverrideStartedV1(
        Guid CityId,
        WeatherStateV1 ForcedState,
        string Source,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        string? Reason,
        DateTime OccurredOnUtc);
}
