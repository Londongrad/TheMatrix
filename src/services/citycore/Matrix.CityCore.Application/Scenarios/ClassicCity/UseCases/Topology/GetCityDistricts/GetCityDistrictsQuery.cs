using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts
{
    public sealed record GetCityDistrictsQuery(Guid CityId) : IRequest<IReadOnlyList<DistrictDto>>;
}
