using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string SimulationCoreService = "SimulationCore";
        private const string ClassicCityGroup = "Classic City";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
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
                    Description: "Retry failed classic city population bootstrap.")
            };
    }
}
