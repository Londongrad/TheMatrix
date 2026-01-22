using Matrix.CityCore.Application.UseCases.Cities.Common;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.GetCity
{
    public sealed record GetCityQuery(Guid CityId) : IRequest<CityDto?>;
}
