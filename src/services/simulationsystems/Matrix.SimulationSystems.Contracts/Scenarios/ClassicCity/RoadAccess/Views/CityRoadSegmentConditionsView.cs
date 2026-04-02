namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views
{
    public sealed record CityRoadSegmentConditionsView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal RoadSupportIndex,
        IReadOnlyList<CityRoadSegmentConditionView> Segments);
}
