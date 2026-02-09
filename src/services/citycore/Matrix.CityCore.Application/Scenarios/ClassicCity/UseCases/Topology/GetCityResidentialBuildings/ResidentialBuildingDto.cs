using Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings
{
    public sealed record ResidentialBuildingDto(
        Guid ResidentialBuildingId,
        Guid CityId,
        Guid DistrictId,
        string Name,
        string Type,
        int ResidentCapacity,
        DateTimeOffset CreatedAtUtc)
    {
        public static ResidentialBuildingDto FromDomain(ResidentialBuilding building)
        {
            return new ResidentialBuildingDto(
                ResidentialBuildingId: building.Id.Value,
                CityId: building.CityId.Value,
                DistrictId: building.DistrictId.Value,
                Name: building.Name.Value,
                Type: building.Type.ToString(),
                ResidentCapacity: building.ResidentCapacity.Value,
                CreatedAtUtc: building.CreatedAtUtc);
        }
    }
}
