using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles
{
    public sealed record GetCityStockpilesQuery(Guid CityId) : IRequest<CityStockpilesDto?>;
}
