namespace Matrix.BuildingBlocks.Application.Authorization
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";
        private const string IdentityService = "Identity";

        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            new(
                Key: PermissionKeys.IdentityUsersRead,
                Service: IdentityService,
                Group: "Users",
                Description: "View users list"),
            new(
                Key: PermissionKeys.PopulationPeopleRead,
                Service: PopulationService,
                Group: "People",
                Description: "View people"),
            new(
                Key: PermissionKeys.PopulationPeopleKill,
                Service: PopulationService,
                Group: "People",
                Description: "Kill a person"),
            new(
                Key: PermissionKeys.PopulationPeopleResurrect,
                Service: PopulationService,
                Group: "People",
                Description: "Resurrect a person")
        ];

        public static IReadOnlyCollection<string> AllKeys =>
            All.Select(x => x.Key)
               .ToArray();

        public static PermissionDefinition GetOrThrow(string key)
        {
            PermissionDefinition? item = All.FirstOrDefault(x => x.Key == key);
            return item ?? throw new InvalidOperationException($"Permission not found in catalog: {key}");
        }
    }
}
