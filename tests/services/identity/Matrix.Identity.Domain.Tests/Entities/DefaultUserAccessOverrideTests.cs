using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
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

            Assert.Equal(
                expected: policyId,
                actual: accessOverride.PolicyId);
            Assert.Equal(
                expected: "identity.users.read",
                actual: accessOverride.PermissionKey);
            Assert.Equal(
                expected: PermissionEffect.Allow,
                actual: accessOverride.Effect);
        }

        [Fact]
        public void Constructor_WithInvalidValues_ThrowsDomainException()
        {
            DomainException emptyPolicyException = Assert.Throws<DomainException>(() => new DefaultUserAccessOverride(
                policyId: Guid.Empty,
                permissionKey: "identity.users.read",
                effect: PermissionEffect.Allow));

            Assert.Equal(
                expected: "Identity.Common.EmptyId",
                actual: emptyPolicyException.Code);
            Assert.Equal(
                expected: "policyId",
                actual: emptyPolicyException.PropertyName);

            DomainException emptyPermissionException = Assert.Throws<DomainException>(()
                => new DefaultUserAccessOverride(
                    policyId: Guid.Parse("80000000-0000-0000-0000-000000000020"),
                    permissionKey: " ",
                    effect: PermissionEffect.Deny));

            Assert.Equal(
                expected: "Identity.Permission.Key.Empty",
                actual: emptyPermissionException.Code);
            Assert.Equal(
                expected: "permissionKey",
                actual: emptyPermissionException.PropertyName);

            DomainException longPermissionException = Assert.Throws<DomainException>(()
                => new DefaultUserAccessOverride(
                    policyId: Guid.Parse("80000000-0000-0000-0000-000000000020"),
                    permissionKey: new string(
                        c: 'p',
                        count: DefaultUserAccessOverride.PermissionKeyMaxLength + 1),
                    effect: PermissionEffect.Deny));

            Assert.Equal(
                expected: "Identity.Permission.Key.InvalidLength",
                actual: longPermissionException.Code);
            Assert.Equal(
                expected: "permissionKey",
                actual: longPermissionException.PropertyName);
        }

        [Fact]
        public void SetEffect_UpdatesEffect()
        {
            var accessOverride = new DefaultUserAccessOverride(
                policyId: Guid.Parse("80000000-0000-0000-0000-000000000020"),
                permissionKey: "identity.users.read",
                effect: PermissionEffect.Allow);

            accessOverride.SetEffect(PermissionEffect.Deny);

            Assert.Equal(
                expected: PermissionEffect.Deny,
                actual: accessOverride.Effect);
        }
    }
}
