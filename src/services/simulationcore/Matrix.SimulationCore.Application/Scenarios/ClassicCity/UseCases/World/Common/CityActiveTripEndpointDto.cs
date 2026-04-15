namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common
{
    public sealed record CityActiveTripEndpointDto(
        string Kind,
        Guid EntityId,
        Guid DistrictId,
        Guid RoadNodeId,
        string Name,
        decimal PositionX,
        decimal PositionY);
}
