namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views
{
    public sealed record CityActiveTripEndpointView(
        string Kind,
        Guid EntityId,
        Guid DistrictId,
        Guid RoadNodeId,
        string Name,
        decimal PositionX,
        decimal PositionY);
}
