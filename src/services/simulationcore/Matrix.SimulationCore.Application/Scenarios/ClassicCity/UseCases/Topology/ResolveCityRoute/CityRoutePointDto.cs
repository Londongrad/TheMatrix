namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public sealed record CityRoutePointDto(
        string Kind,
        Guid EntityId,
        Guid DistrictId,
        Guid RoadNodeId,
        string Name,
        decimal PositionX,
        decimal PositionY);
}
