using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity
{
    public sealed record RenameCityCommand(
        Guid CityId,
        string Name) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityUpdate;
    }
}
