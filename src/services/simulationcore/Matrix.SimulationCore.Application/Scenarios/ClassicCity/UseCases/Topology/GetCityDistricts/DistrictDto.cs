using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts
{
    public sealed record DistrictDto(
        Guid DistrictId,
        Guid CityId,
        string Name,
        decimal AnchorX,
        decimal AnchorY,
        DateTimeOffset CreatedAtUtc)
    {
        public static DistrictDto FromDomain(District district)
        {
            return new DistrictDto(
                DistrictId: district.Id.Value,
                CityId: district.CityId.Value,
                Name: district.Name.Value,
                AnchorX: district.AnchorX,
                AnchorY: district.AnchorY,
                CreatedAtUtc: district.CreatedAtUtc);
        }
    }
}
