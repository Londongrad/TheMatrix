using Matrix.CityCore.Application.UseCases.Cities.Common;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.ListCities
{
    public sealed record ListCitiesQuery(bool IncludeArchived) : IRequest<IReadOnlyList<CityDto>>;
}
