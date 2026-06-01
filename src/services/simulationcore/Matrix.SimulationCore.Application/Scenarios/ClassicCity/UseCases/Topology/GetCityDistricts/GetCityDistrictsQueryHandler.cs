using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts
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
