using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class DefaultUserAccessOverrideTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var policyId = Guid.Parse("80000000-0000-0000-0000-000000000020");
        var accessOverride = new DefaultUserAccessOverride(
            policyId: policyId,
            permissionKey: "identity.users.read",
            effect: PermissionEffect.Allow);

        Assert.Equal(policyId, accessOverride.PolicyId);
        Assert.Equal("identity.users.read", accessOverride.PermissionKey);
        Assert.Equal(PermissionEffect.Allow, accessOverride.Effect);
    }

    [Fact]
    public void Constructor_WithInvalidValues_ThrowsDomainException()
    {
        var emptyPolicyException = Assert.Throws<DomainException>(() => new DefaultUserAccessOverride(
            policyId: Guid.Empty,
            permissionKey: "identity.users.read",
            effect: PermissionEffect.Allow));

        Assert.Equal("Identity.Common.EmptyId", emptyPolicyException.Code);
        Assert.Equal("policyId", emptyPolicyException.PropertyName);

        var emptyPermissionException = Assert.Throws<DomainException>(() => new DefaultUserAccessOverride(
            policyId: Guid.Parse("80000000-0000-0000-0000-000000000020"),
            permissionKey: " ",
            effect: PermissionEffect.Deny));

        Assert.Equal("Identity.Permission.Key.Empty", emptyPermissionException.Code);
        Assert.Equal("permissionKey", emptyPermissionException.PropertyName);

        var longPermissionException = Assert.Throws<DomainException>(() => new DefaultUserAccessOverride(
            policyId: Guid.Parse("80000000-0000-0000-0000-000000000020"),
            permissionKey: new string('p', DefaultUserAccessOverride.PermissionKeyMaxLength + 1),
            effect: PermissionEffect.Deny));

        Assert.Equal("Identity.Permission.Key.InvalidLength", longPermissionException.Code);
        Assert.Equal("permissionKey", longPermissionException.PropertyName);
    }

    [Fact]
    public void SetEffect_UpdatesEffect()
    {
        var accessOverride = new DefaultUserAccessOverride(
            policyId: Guid.Parse("80000000-0000-0000-0000-000000000020"),
            permissionKey: "identity.users.read",
            effect: PermissionEffect.Allow);

        accessOverride.SetEffect(PermissionEffect.Deny);

        Assert.Equal(PermissionEffect.Deny, accessOverride.Effect);
    }
}
