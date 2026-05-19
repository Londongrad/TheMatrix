using Matrix.PermissionCatalog.Abstractions;
using Xunit;

namespace Matrix.PermissionCatalog.Tests;

public sealed class ApplicationPermissionsCatalogTests
{
    [Fact]
    public void All_WhenBuilt_ContainsUniqueKeysWithNoBlankFields()
    {
        IReadOnlyList<PermissionDefinition> permissions = ApplicationPermissionsCatalog.All;

        Assert.NotEmpty(permissions);
        Assert.Equal(
            permissions.Count,
            permissions.Select(x => x.Key)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.DoesNotContain(permissions, permission =>
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
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "Economy",
            "Identity",
            "Population",
            "Resources",
            "SimulationCore",
            "SimulationSystems"
        ], services);
    }

    [Fact]
    public void All_WhenBuilt_ContainsRepresentativePermissionsAcrossServices()
    {
        string[] keys = ApplicationPermissionsCatalog.All
            .Select(x => x.Key)
            .ToArray();

        Assert.Contains(keys, key => key.StartsWith("simulationcore.", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.StartsWith("identity.", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.StartsWith("population.", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.StartsWith("economy.", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.StartsWith("resources.", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.StartsWith("simulationsystems.", StringComparison.Ordinal));
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

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }
}
