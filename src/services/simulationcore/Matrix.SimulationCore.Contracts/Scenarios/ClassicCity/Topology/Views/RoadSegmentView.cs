namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record RoadSegmentView(
        Guid RoadSegmentId,
        Guid CityId,
        Guid DistrictId,
        Guid FromRoadNodeId,
        Guid ToRoadNodeId,
        string Name,
        string Type,
        decimal LengthMeters,
        DateTimeOffset CreatedAtUtc);
}
