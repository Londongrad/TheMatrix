using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Resources.Application.Authorization.Permissions;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles
{
    public sealed record GetCityStockpilesQuery(Guid CityId)
        : IRequest<CityStockpilesDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.ResourcesClassicCityRead;
    }
}
