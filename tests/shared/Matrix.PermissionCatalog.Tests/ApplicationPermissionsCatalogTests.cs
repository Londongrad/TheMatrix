using Matrix.PermissionCatalog.Abstractions;
using Xunit;

namespace Matrix.PermissionCatalog.Tests
{
    public sealed class ApplicationPermissionsCatalogTests
    {
        [Fact]
        public void All_WhenBuilt_ContainsUniqueKeysWithNoBlankFields()
        {
            IReadOnlyList<PermissionDefinition> permissions = ApplicationPermissionsCatalog.All;

            Assert.NotEmpty(permissions);
            Assert.Equal(
                expected: permissions.Count,
                actual: permissions.Select(x => x.Key)
                   .Distinct(StringComparer.Ordinal)
                   .Count());
            Assert.DoesNotContain(
                collection: permissions,
                filter: permission =>
                    string.IsNullOrWhiteSpace(permission.Key) ||
                    string.IsNullOrWhiteSpace(permission.Service) ||
                    string.IsNullOrWhiteSpace(permission.Group) ||
                    string.IsNullOrWhiteSpace(permission.Description));
        }

        [Fact]
        public void All_WhenBuilt_ContainsPermissionsForEveryKnownService()
        {
            string[] services = ApplicationPermissionsCatalog.All
               .Select(x => x.Service)
               .Distinct(StringComparer.Ordinal)
               .OrderBy(
                    keySelector: x => x,
                    comparer: StringComparer.Ordinal)
               .ToArray();

            Assert.Equal(
                expectedSpan:
                [
                    "Economy",
                    "Identity",
                    "Population",
                    "Resources",
                    "SimulationCore",
                    "SimulationSystems"
                ],
                actualArray: services);
        }

        [Fact]
        public void All_WhenBuilt_ContainsRepresentativePermissionsAcrossServices()
        {
            string[] keys = ApplicationPermissionsCatalog.All
               .Select(x => x.Key)
               .ToArray();

            Assert.Contains(
                collection: keys,
                filter: key => key.StartsWith(
                    value: "simulationcore.",
                    comparisonType: StringComparison.Ordinal));
            Assert.Contains(
                collection: keys,
                filter: key => key.StartsWith(
                    value: "identity.",
                    comparisonType: StringComparison.Ordinal));
            Assert.Contains(
                collection: keys,
                filter: key => key.StartsWith(
                    value: "population.",
                    comparisonType: StringComparison.Ordinal));
            Assert.Contains(
                collection: keys,
                filter: key => key.StartsWith(
                    value: "economy.",
                    comparisonType: StringComparison.Ordinal));
            Assert.Contains(
                collection: keys,
                filter: key => key.StartsWith(
                    value: "resources.",
                    comparisonType: StringComparison.Ordinal));
            Assert.Contains(
                collection: keys,
                filter: key => key.StartsWith(
                    value: "simulationsystems.",
                    comparisonType: StringComparison.Ordinal));
        }

        [Fact]
        public void PermissionDefinition_RecordEquality_UsesValueSemantics()
        {
            PermissionDefinition left = new(
                Key: "identity.users.read",
                Service: "Identity",
                Group: "Admin / Users",
                Description: "View user list.");
            PermissionDefinition right = new(
                Key: "identity.users.read",
                Service: "Identity",
                Group: "Admin / Users",
                Description: "View user list.");

            Assert.Equal(
                expected: left,
                actual: right);
            Assert.Equal(
                expected: left.GetHashCode(),
                actual: right.GetHashCode());
        }
    }
}
