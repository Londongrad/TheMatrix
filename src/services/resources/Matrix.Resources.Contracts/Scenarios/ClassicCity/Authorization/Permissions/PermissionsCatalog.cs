using Matrix.PermissionCatalog.Abstractions;

namespace Matrix.Resources.Contracts.Scenarios.ClassicCity.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string ResourcesService = "Resources";
        private const string ClassicCityGroup = "Classic City";

        public static readonly IReadOnlyList<PermissionDefinition> All =
            new List<PermissionDefinition>
            {
                new(
                    Key: PermissionKeys.ResourcesClassicCityRead,
                    Service: ResourcesService,
                    Group: ClassicCityGroup,
                    Description: "View classic city stockpiles and supply pressure."),
                new(
                    Key: PermissionKeys.ResourcesClassicCityManage,
                    Service: ResourcesService,
                    Group: ClassicCityGroup,
                    Description: "Manage classic city rationing and resupply operations.")
            };
    }
}
