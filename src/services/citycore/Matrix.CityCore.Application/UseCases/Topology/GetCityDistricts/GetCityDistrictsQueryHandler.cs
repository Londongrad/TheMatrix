using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Topology.GetCityDistricts
{
    public sealed class GetCityDistrictsQueryHandler(IDistrictRepository repository)
        : IRequestHandler<GetCityDistrictsQuery, IReadOnlyList<DistrictDto>>
    {
        public async Task<IReadOnlyList<DistrictDto>> Handle(
            GetCityDistrictsQuery request,
            CancellationToken cancellationToken)
        {
            return (await repository.ListByCityIdAsync(
                    cityId: new CityId(request.CityId),
                    cancellationToken: cancellationToken))
               .Select(DistrictDto.FromDomain)
               .ToArray();
        }
    }
}
