using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed record RestartCityPopulationBootstrapCommand(
        Guid CityId,
        int? PlannedPeopleCountOverride = null)
        : IRequest<RestartCityPopulationBootstrapResult>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.SimulationCoreClassicCityRead,
            AppPermissionKeys.SimulationCoreClassicCityPopulationBootstrapRetry
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
