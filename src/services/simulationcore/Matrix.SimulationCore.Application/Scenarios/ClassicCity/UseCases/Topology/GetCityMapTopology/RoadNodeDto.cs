using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed record RoadNodeDto(
        Guid RoadNodeId,
        Guid CityId,
        Guid DistrictId,
        string Name,
        string Type,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc)
    {
        public static RoadNodeDto FromDomain(RoadNode roadNode)
        {
            return new RoadNodeDto(
                RoadNodeId: roadNode.Id.Value,
                CityId: roadNode.CityId.Value,
                DistrictId: roadNode.DistrictId.Value,
                Name: roadNode.Name,
                Type: roadNode.Type.ToString(),
                PositionX: roadNode.PositionX,
                PositionY: roadNode.PositionY,
                CreatedAtUtc: roadNode.CreatedAtUtc);
        }
    }
}
