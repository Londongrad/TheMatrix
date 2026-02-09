using Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities
{
    public sealed record ListCitiesQuery(bool IncludeArchived) : IRequest<IReadOnlyList<CityDto>>;
}
