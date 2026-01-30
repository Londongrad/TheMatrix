using MediatR;

namespace Matrix.CityCore.Application.UseCases.Topology.GetCityDistricts
{
    public sealed record GetCityDistrictsQuery(Guid CityId) : IRequest<IReadOnlyList<DistrictDto>>;
}
