using Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts
{
    public sealed record DistrictDto(
        Guid DistrictId,
        Guid CityId,
        string Name,
        DateTimeOffset CreatedAtUtc)
    {
        public static DistrictDto FromDomain(District district)
        {
            return new DistrictDto(
                DistrictId: district.Id.Value,
                CityId: district.CityId.Value,
                Name: district.Name.Value,
                CreatedAtUtc: district.CreatedAtUtc);
        }
    }
}
