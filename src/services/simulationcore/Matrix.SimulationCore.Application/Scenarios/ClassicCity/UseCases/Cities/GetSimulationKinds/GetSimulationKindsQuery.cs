using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.SimulationCore.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds
{
    public sealed record GetSimulationKindsQuery
        : IRequest<IReadOnlyList<SimulationKindCatalogItemDto>>, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.SimulationCoreScenariosCatalogRead,
            AppPermissionKeys.SimulationCoreClassicCityCreate
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.Any;
    }
}
