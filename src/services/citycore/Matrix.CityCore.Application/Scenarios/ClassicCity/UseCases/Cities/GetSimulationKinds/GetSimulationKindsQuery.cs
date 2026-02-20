using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using AppPermissionKeys = Matrix.CityCore.Application.Authorization.Permissions.PermissionKeys;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds
{
    public sealed record GetSimulationKindsQuery
        : IRequest<IReadOnlyList<SimulationKindCatalogItemDto>>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.CityCoreScenariosCatalogRead,
            AppPermissionKeys.CityCoreClassicCityCreate
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.Any;
    }
}
