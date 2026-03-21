using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities
{
    public sealed class ListCitiesQueryHandler(ICityRepository cityRepository)
        : IRequestHandler<ListCitiesQuery, IReadOnlyList<CityDto>>
    {
        public async Task<IReadOnlyList<CityDto>> Handle(
            ListCitiesQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<City> cities = await cityRepository.ListAsync(
                includeArchived: request.IncludeArchived,
                cancellationToken: cancellationToken);

            return cities
               .Select(CityDto.FromDomain)
               .ToList();
        }
    }
}
