using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.SimulationCore.Contracts.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string SimulationCoreService = "SimulationCore";

        private const string ScenariosGroup = "Scenarios";
        private const string ClassicCityGroup = "Classic City";
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
                    Key: PermissionKeys.SimulationCoreClassicCityRead,
                    Service: SimulationCoreService,
                    Group: ClassicCityGroup,
                    Description: "View classic city workspaces, topology, weather, and provisioning state."),
                new(
                    Key: PermissionKeys.SimulationCoreClassicCityCreate,
                    Service: SimulationCoreService,
                    Group: ClassicCityGroup,
                    Description: "Create new classic city simulations."),
                new(
                    Key: PermissionKeys.SimulationCoreClassicCityUpdate,
                    Service: SimulationCoreService,
                    Group: ClassicCityGroup,
                    Description: "Rename cities and update classic city environment settings."),
                new(
                    Key: PermissionKeys.SimulationCoreClassicCityArchive,
                    Service: SimulationCoreService,
                    Group: ClassicCityGroup,
                    Description: "Archive classic city simulations."),
                new(
                    Key: PermissionKeys.SimulationCoreClassicCityDelete,
                    Service: SimulationCoreService,
                    Group: ClassicCityGroup,
                    Description: "Delete archived classic city simulations."),
                new(
                    Key: PermissionKeys.SimulationCoreClassicCityPopulationBootstrapRetry,
                    Service: SimulationCoreService,
                    Group: ClassicCityGroup,
                    Description: "Retry failed classic city population bootstrap."),
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
