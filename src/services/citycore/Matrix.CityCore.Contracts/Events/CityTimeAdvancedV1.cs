namespace Matrix.CityCore.Contracts.Events
{
    public sealed record CityTimeAdvancedV1(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId,
        decimal SpeedMultiplier,
        DateTime OccurredOnUtc);
}
