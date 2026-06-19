using Matrix.PermissionCatalog.Abstractions;
using EconomyPermissionsCatalog = Matrix.Economy.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionsCatalog;
using IdentityPermissionsCatalog = Matrix.Identity.Contracts.Authorization.Permissions.PermissionsCatalog;
using PopulationPermissionsCatalog = Matrix.Population.Contracts.Authorization.Permissions.PermissionsCatalog;
using PopulationClassicCityPermissionsCatalog =
    Matrix.Population.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionsCatalog;
using ResourcesPermissionsCatalog = Matrix.Resources.Contracts.Authorization.Permissions.PermissionsCatalog;
using SimulationCorePermissionsCatalog = Matrix.SimulationCore.Contracts.Authorization.Permissions.PermissionsCatalog;
using SimulationCoreClassicCityPermissionsCatalog =
    Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionsCatalog;
using SimulationSystemsPermissionsCatalog =
    Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionsCatalog;

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
                definitions: SimulationCorePermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: SimulationCoreClassicCityPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: IdentityPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: PopulationPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: PopulationClassicCityPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: EconomyPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: ResourcesPermissionsCatalog.All);

            AddRange(
                permissionsByKey: permissionsByKey,
                definitions: SimulationSystemsPermissionsCatalog.All);

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
