using Matrix.PermissionCatalog.Abstractions;
using IdentityPermissionsCatalog = Matrix.Identity.Contracts.Authorization.Permissions.PermissionsCatalog;
using PopulationPermissionsCatalog = Matrix.Population.Contracts.Authorization.Permissions.PermissionsCatalog;

namespace Matrix.PermissionCatalog
{
    public static class ApplicationPermissionsCatalog
    {
        public static IReadOnlyList<PermissionDefinition> All { get; } = Build();

        private static IReadOnlyList<PermissionDefinition> Build()
        {
            var permissionsByKey = new Dictionary<string, PermissionDefinition>(StringComparer.Ordinal);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: IdentityPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: PopulationPermissionsCatalog.All);

            return permissionsByKey.Values.ToArray();
        }

        private static void AddRange(
            Dictionary<string, PermissionDefinition> permissionsByKey,
            IReadOnlyCollection<PermissionDefinition> definitions)
        {
            foreach (PermissionDefinition definition in definitions)
                permissionsByKey[definition.Key] = definition;
        }
    }
}
