using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed record RestartCityPopulationBootstrapCommand(Guid CityId)
        : IRequest<RestartCityPopulationBootstrapResult>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreClassicCityRead,
            AppPermissionKeys.CityCoreClassicCityPopulationBootstrapRetry
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
