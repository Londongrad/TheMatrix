using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.Population.Contracts.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";

        private const string PeopleGroup = "People";
        private const string PersonGroup = "Person";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new(
                    Key: PermissionKeys.PopulationPeopleRead,
                    Service: PopulationService,
                    Group: PeopleGroup,
                    Description: "Read people page."),
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
            };
    }
}
