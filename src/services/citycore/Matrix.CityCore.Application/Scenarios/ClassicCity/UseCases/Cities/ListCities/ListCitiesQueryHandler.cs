using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities
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
