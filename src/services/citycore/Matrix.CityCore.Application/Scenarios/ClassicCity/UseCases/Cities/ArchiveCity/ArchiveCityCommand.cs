using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity
{
    public sealed record ArchiveCityCommand(Guid CityId) : IRequest<bool>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreClassicCityRead,
            AppPermissionKeys.CityCoreClassicCityArchive
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
