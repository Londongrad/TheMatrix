using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment
{
    public sealed record UpdateCityEnvironmentCommand(
        Guid CityId,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreClassicCityRead,
            AppPermissionKeys.CityCoreClassicCityUpdate
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
