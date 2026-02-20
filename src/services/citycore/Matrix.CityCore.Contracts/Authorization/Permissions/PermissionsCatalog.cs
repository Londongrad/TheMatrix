using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.CityCore.Contracts.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string CityCoreService = "CityCore";

        private const string ScenariosGroup = "Scenarios";
        private const string ClassicCityGroup = "Classic City";
        private const string SimulationsGroup = "Simulations";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new(
                    Key: PermissionKeys.CityCoreScenariosCatalogRead,
                    Service: CityCoreService,
                    Group: ScenariosGroup,
                    Description: "View available simulation scenarios."),
                new(
                    Key: PermissionKeys.CityCoreClassicCityRead,
                    Service: CityCoreService,
                    Group: ClassicCityGroup,
                    Description: "View classic city workspaces, topology, weather, and provisioning state."),
                new(
                    Key: PermissionKeys.CityCoreClassicCityCreate,
                    Service: CityCoreService,
                    Group: ClassicCityGroup,
                    Description: "Create new classic city simulations."),
                new(
                    Key: PermissionKeys.CityCoreClassicCityUpdate,
                    Service: CityCoreService,
                    Group: ClassicCityGroup,
                    Description: "Rename cities and update classic city environment settings."),
                new(
                    Key: PermissionKeys.CityCoreClassicCityArchive,
                    Service: CityCoreService,
                    Group: ClassicCityGroup,
                    Description: "Archive classic city simulations."),
                new(
                    Key: PermissionKeys.CityCoreClassicCityDelete,
                    Service: CityCoreService,
                    Group: ClassicCityGroup,
                    Description: "Delete archived classic city simulations."),
                new(
                    Key: PermissionKeys.CityCoreClassicCityPopulationBootstrapRetry,
                    Service: CityCoreService,
                    Group: ClassicCityGroup,
                    Description: "Retry failed classic city population bootstrap."),
                new(
                    Key: PermissionKeys.CityCoreSimulationRead,
                    Service: CityCoreService,
                    Group: SimulationsGroup,
                    Description: "View simulation clock state."),
                new(
                    Key: PermissionKeys.CityCoreSimulationControl,
                    Service: CityCoreService,
                    Group: SimulationsGroup,
                    Description: "Pause, resume, retime, and change simulation speed.")
            };
    }
}
