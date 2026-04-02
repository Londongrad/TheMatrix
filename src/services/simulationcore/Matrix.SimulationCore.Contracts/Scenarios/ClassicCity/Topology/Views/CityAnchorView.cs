namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views
{
    public sealed record CityAnchorView(
        Guid CityAnchorId,
        Guid CityId,
        Guid DistrictId,
        Guid AccessRoadNodeId,
        string Name,
        string Type,
        int Capacity,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc);
}
