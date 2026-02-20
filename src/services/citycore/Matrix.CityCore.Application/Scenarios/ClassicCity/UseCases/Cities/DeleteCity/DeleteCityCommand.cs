using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity
{
    public sealed record DeleteCityCommand(Guid CityId)
        : IRequest<DeleteCityResult>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreClassicCityRead,
            AppPermissionKeys.CityCoreClassicCityDelete
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
