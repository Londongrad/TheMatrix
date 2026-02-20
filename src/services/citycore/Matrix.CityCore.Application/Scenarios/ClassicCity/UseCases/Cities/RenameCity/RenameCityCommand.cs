using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity
{
    public sealed record RenameCityCommand(
        Guid CityId,
        string Name) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreClassicCityRead,
            AppPermissionKeys.CityCoreClassicCityUpdate
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
