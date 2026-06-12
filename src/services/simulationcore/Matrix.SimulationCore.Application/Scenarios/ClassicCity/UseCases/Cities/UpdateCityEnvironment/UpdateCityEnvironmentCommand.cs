using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment
{
    public sealed record UpdateCityEnvironmentCommand(
        Guid CityId,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.SimulationCoreClassicCityRead,
            AppPermissionKeys.SimulationCoreClassicCityUpdate
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
