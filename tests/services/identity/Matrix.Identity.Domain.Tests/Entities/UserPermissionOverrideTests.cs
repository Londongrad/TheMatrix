using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class UserPermissionOverrideTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var userId = Guid.Parse("80000000-0000-0000-0000-000000000010");
        var permissionOverride = new UserPermissionOverride(
            userId: userId,
            permissionKey: "identity.users.read",
            effect: PermissionEffect.Allow);

        Assert.Equal(userId, permissionOverride.UserId);
        Assert.Equal("identity.users.read", permissionOverride.PermissionKey);
        Assert.Equal(PermissionEffect.Allow, permissionOverride.Effect);
    }

    [Fact]
    public void Constructor_WithInvalidValues_ThrowsDomainException()
    {
        var emptyUserException = Assert.Throws<DomainException>(() => new UserPermissionOverride(
            userId: Guid.Empty,
            permissionKey: "identity.users.read",
            effect: PermissionEffect.Allow));

        Assert.Equal("Identity.User.EmptyId", emptyUserException.Code);
        Assert.Equal("userId", emptyUserException.PropertyName);

        var emptyPermissionException = Assert.Throws<DomainException>(() => new UserPermissionOverride(
            userId: Guid.Parse("80000000-0000-0000-0000-000000000010"),
            permissionKey: "   ",
            effect: PermissionEffect.Deny));

        Assert.Equal("Identity.Permission.Key.Empty", emptyPermissionException.Code);
        Assert.Equal("permissionKey", emptyPermissionException.PropertyName);

        var longPermissionException = Assert.Throws<DomainException>(() => new UserPermissionOverride(
            userId: Guid.Parse("80000000-0000-0000-0000-000000000010"),
            permissionKey: new string('p', UserPermissionOverride.PermissionKeyMaxLength + 1),
            effect: PermissionEffect.Deny));

        Assert.Equal("Identity.Permission.Key.InvalidLength", longPermissionException.Code);
        Assert.Equal("permissionKey", longPermissionException.PropertyName);
    }

    [Fact]
    public void SetEffect_UpdatesEffect()
    {
        var permissionOverride = new UserPermissionOverride(
            userId: Guid.Parse("80000000-0000-0000-0000-000000000010"),
            permissionKey: "identity.users.read",
            effect: PermissionEffect.Allow);

        permissionOverride.SetEffect(PermissionEffect.Deny);

        Assert.Equal(PermissionEffect.Deny, permissionOverride.Effect);
    }
}
