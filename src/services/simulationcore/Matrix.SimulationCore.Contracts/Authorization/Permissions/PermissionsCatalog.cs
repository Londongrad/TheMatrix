using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.SimulationCore.Contracts.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string SimulationCoreService = "SimulationCore";

        private const string ScenariosGroup = "Scenarios";
        private const string SimulationsGroup = "Simulations";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new(
                    Key: PermissionKeys.SimulationCoreScenariosCatalogRead,
                    Service: SimulationCoreService,
                    Group: ScenariosGroup,
                    Description: "View available simulation scenarios."),
                new(
                    Key: PermissionKeys.SimulationCoreSimulationRead,
                    Service: SimulationCoreService,
                    Group: SimulationsGroup,
                    Description: "View simulation clock state."),
                new(
                    Key: PermissionKeys.SimulationCoreSimulationControl,
                    Service: SimulationCoreService,
                    Group: SimulationsGroup,
                    Description: "Pause, resume, retime, and change simulation speed.")
            };
    }
}
