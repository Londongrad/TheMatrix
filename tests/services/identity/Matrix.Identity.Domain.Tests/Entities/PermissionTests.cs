using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class PermissionTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var permission = new Permission(
            key: "identity.users.read",
            service: "identity",
            group: "users",
            description: "Read users.");

        Assert.Equal("identity.users.read", permission.Key);
        Assert.Equal("identity", permission.Service);
        Assert.Equal("users", permission.Group);
        Assert.Equal("Read users.", permission.Description);
        Assert.False(permission.IsDeprecated);
    }

    [Fact]
    public void UpdateMetadata_UpdatesProperties()
    {
        var permission = new Permission(
            key: "identity.users.read",
            service: "identity",
            group: "users",
            description: "Read users.");

        permission.UpdateMetadata(
            service: "iam",
            group: "accounts",
            description: "Read account data.");

        Assert.Equal("iam", permission.Service);
        Assert.Equal("accounts", permission.Group);
        Assert.Equal("Read account data.", permission.Description);
    }

    [Fact]
    public void Deprecate_AndActivate_ToggleDeprecatedFlag()
    {
        var permission = new Permission(
            key: "identity.users.read",
            service: "identity",
            group: "users",
            description: "Read users.");

        permission.Deprecate();
        Assert.True(permission.IsDeprecated);

        permission.Activate();
        Assert.False(permission.IsDeprecated);
    }
}
