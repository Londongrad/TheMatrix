using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings
{
    public sealed record ResidentialBuildingDto(
        Guid ResidentialBuildingId,
        Guid CityId,
        Guid DistrictId,
        Guid AccessRoadNodeId,
        string Name,
        string Type,
        int ResidentCapacity,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc)
    {
        public static ResidentialBuildingDto FromDomain(ResidentialBuilding building)
        {
            return new ResidentialBuildingDto(
                ResidentialBuildingId: building.Id.Value,
                CityId: building.CityId.Value,
                DistrictId: building.DistrictId.Value,
                AccessRoadNodeId: building.AccessRoadNodeId.Value,
                Name: building.Name.Value,
                Type: building.Type.ToString(),
                ResidentCapacity: building.ResidentCapacity.Value,
                PositionX: building.PositionX,
                PositionY: building.PositionY,
                CreatedAtUtc: building.CreatedAtUtc);
        }
    }
}
