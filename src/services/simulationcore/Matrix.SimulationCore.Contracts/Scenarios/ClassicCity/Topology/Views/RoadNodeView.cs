namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record RoadNodeView(
        Guid RoadNodeId,
        Guid CityId,
        Guid DistrictId,
        string Name,
        string Type,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc);
}
