using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity
{
    public sealed record ArchiveCityCommand(Guid CityId) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityArchive;
    }
}
