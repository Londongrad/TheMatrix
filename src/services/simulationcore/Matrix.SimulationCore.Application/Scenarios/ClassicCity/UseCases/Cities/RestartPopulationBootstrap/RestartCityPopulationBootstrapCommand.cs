using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
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
