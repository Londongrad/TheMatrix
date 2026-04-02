using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors
{
    public sealed record CityAnchorDto(
        Guid CityAnchorId,
        Guid CityId,
        Guid DistrictId,
        Guid AccessRoadNodeId,
        string Name,
        string Type,
        int Capacity,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc)
    {
        public static CityAnchorDto FromDomain(CityAnchor anchor)
        {
            return new CityAnchorDto(
                CityAnchorId: anchor.Id.Value,
                CityId: anchor.CityId.Value,
                DistrictId: anchor.DistrictId.Value,
                AccessRoadNodeId: anchor.AccessRoadNodeId.Value,
                Name: anchor.Name.Value,
                Type: anchor.Type.ToString(),
                Capacity: anchor.Capacity,
                PositionX: anchor.PositionX,
                PositionY: anchor.PositionY,
                CreatedAtUtc: anchor.CreatedAtUtc);
        }
    }
}
