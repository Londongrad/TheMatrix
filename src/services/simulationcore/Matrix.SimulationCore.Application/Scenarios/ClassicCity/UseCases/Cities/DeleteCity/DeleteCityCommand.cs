using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity
{
    public sealed record DeleteCityCommand(Guid CityId)
        : IRequest<DeleteCityResult>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.SimulationCoreClassicCityRead,
            AppPermissionKeys.SimulationCoreClassicCityDelete
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
