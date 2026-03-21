using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListProvisioningCities
{
    public sealed class ListProvisioningCitiesQueryHandler(ICityRepository cityRepository)
        : IRequestHandler<ListProvisioningCitiesQuery, IReadOnlyList<CityDto>>
    {
        public async Task<IReadOnlyList<CityDto>> Handle(
            ListProvisioningCitiesQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<City> cities = await cityRepository.ListProvisioningAsync(
                cancellationToken: cancellationToken);

            return cities
               .Select(CityDto.FromDomain)
               .ToList();
        }
    }
}
