using Matrix.BuildingBlocks.Application.Authorization.Permissions;

namespace Matrix.Population.Application.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";

        private const string PeopleGroup = "People";
        private const string PersonGroup = "Person";

        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            new(
                Key: PermissionKeys.PopulationPeopleRead,
                Service: PopulationService,
                Group: PeopleGroup,
                Description: "Read citizens page."),
            new(
                Key: PermissionKeys.PopulationPeopleInitialize,
                Service: PopulationService,
                Group: PeopleGroup,
                Description: "Initialize population."),
            new(
                Key: PermissionKeys.PopulationPersonKill,
                Service: PopulationService,
                Group: PersonGroup,
                Description: "Kill a person."),
            new(
                Key: PermissionKeys.PopulationPersonResurrect,
                Service: PopulationService,
                Group: PersonGroup,
                Description: "Resurrect a person.")
        ];
    }
}
