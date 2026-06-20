using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Scenarios.ListSimulationScenarios
{
    public sealed record ListSimulationScenariosQuery
        : IRequest<IReadOnlyList<SimulationScenarioDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreScenariosCatalogRead;
    }
}
