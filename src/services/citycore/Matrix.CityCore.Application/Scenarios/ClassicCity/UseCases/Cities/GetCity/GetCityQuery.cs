using Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity
{
    public sealed record GetCityQuery(Guid CityId) : IRequest<CityDto?>;
}
