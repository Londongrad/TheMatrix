using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.Population.Contracts.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";

        private const string PeopleGroup = "People";
        private const string CivilRegistryGroup = "Civil registry";
        private const string EmploymentGroup = "Employment";
        private const string EducationGroup = "Education";
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
                    Key: PermissionKeys.PopulationPeopleInitialize,
                    Service: PopulationService,
                    Group: PeopleGroup,
                    Description: "Initialize population."),
                new(
                    Key: PermissionKeys.PopulationCivilRegistryManage,
                    Service: PopulationService,
                    Group: CivilRegistryGroup,
                    Description: "Manage marriages and divorces inside city civil registry services."),
                new(
                    Key: PermissionKeys.PopulationEmploymentManage,
                    Service: PopulationService,
                    Group: EmploymentGroup,
                    Description: "Manage hiring, firing, and retirement inside classic city employment services."),
                new(
                    Key: PermissionKeys.PopulationEducationManage,
                    Service: PopulationService,
                    Group: EducationGroup,
                    Description:
                    "Manage enrollment, graduation, and study withdrawal inside classic city education services."),
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
