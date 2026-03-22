using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.SimulationSystems.Application.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string SimulationSystemsService = "SimulationSystems";
        private const string ClassicCityGroup = "Classic City";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new(
                    Key: PermissionKeys.SimulationSystemsClassicCityRead,
                    Service: SimulationSystemsService,
                    Group: ClassicCityGroup,
                    Description: "View classic city environmental systems and physical conditions.")
            };
    }
}
