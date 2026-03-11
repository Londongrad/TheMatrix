using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinesses
{
    public sealed record GetCityBusinessesQuery(Guid CityId) : IRequest<IReadOnlyList<CityBusinessDto>>;
}
