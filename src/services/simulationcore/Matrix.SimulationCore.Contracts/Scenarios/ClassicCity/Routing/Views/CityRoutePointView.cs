namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views
{
    public sealed record CityRoutePointView(
        string Kind,
        Guid EntityId,
        Guid DistrictId,
        Guid RoadNodeId,
        string Name,
        decimal PositionX,
        decimal PositionY);
}
