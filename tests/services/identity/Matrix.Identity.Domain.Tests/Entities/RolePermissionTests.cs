using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class RolePermissionTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var roleId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var rolePermission = new RolePermission(
            roleId: roleId,
            permissionKey: "identity.users.read");

        Assert.Equal(roleId, rolePermission.RoleId);
        Assert.Equal("identity.users.read", rolePermission.PermissionKey);
    }

    [Fact]
    public void Constructor_WithEmptyRoleId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new RolePermission(
            roleId: Guid.Empty,
            permissionKey: "identity.users.read"));

        Assert.Equal("Identity.Role.EmptyId", exception.Code);
        Assert.Equal("roleId", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WithWhitespacePermissionKey_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new RolePermission(
            roleId: Guid.Parse("60000000-0000-0000-0000-000000000001"),
            permissionKey: "   "));

        Assert.Equal("Identity.Permission.Key.Empty", exception.Code);
        Assert.Equal("permissionKey", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WithTooLongPermissionKey_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new RolePermission(
            roleId: Guid.Parse("60000000-0000-0000-0000-000000000001"),
            permissionKey: new string('p', RolePermission.PermissionKeyMaxLength + 1)));

        Assert.Equal("Identity.Permission.Key.InvalidLength", exception.Code);
        Assert.Equal("permissionKey", exception.PropertyName);
    }
}
