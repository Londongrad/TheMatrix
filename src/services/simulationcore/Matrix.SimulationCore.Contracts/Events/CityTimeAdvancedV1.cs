namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityTimeAdvancedV1(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId,
        decimal SpeedMultiplier,
        CityTickContextV1 TickContext,
        DateTime OccurredOnUtc);
}
