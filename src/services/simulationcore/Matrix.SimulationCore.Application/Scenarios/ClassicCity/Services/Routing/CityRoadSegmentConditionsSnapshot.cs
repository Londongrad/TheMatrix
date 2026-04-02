namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing
{
    public sealed record CityRoadSegmentConditionsSnapshot(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal RoadSupportIndex,
        IReadOnlyList<CityRoadSegmentConditionSnapshot> Segments);
}
