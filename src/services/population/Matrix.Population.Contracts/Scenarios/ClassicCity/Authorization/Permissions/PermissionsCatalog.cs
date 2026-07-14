using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";

        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            new(
                Key: PermissionKeys.PopulationPeopleInitialize,
                Service: PopulationService,
                Group: "People",
                Description: "Initialize Classic City population."),
            new(
                Key: PermissionKeys.PopulationCivilRegistryManage,
                Service: PopulationService,
                Group: "Civil registry",
                Description: "Manage marriages and divorces inside Classic City civil registry services."),
            new(
                Key: PermissionKeys.PopulationEmploymentManage,
                Service: PopulationService,
                Group: "Employment",
                Description: "Manage hiring, firing, and retirement inside Classic City employment services.")
        ];
    }
}
