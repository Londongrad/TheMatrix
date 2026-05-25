using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
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

            Assert.Equal(
                expected: userId,
                actual: permissionOverride.UserId);
            Assert.Equal(
                expected: "identity.users.read",
                actual: permissionOverride.PermissionKey);
            Assert.Equal(
                expected: PermissionEffect.Allow,
                actual: permissionOverride.Effect);
        }

        [Fact]
        public void Constructor_WithInvalidValues_ThrowsDomainException()
        {
            DomainException emptyUserException = Assert.Throws<DomainException>(() => new UserPermissionOverride(
                userId: Guid.Empty,
                permissionKey: "identity.users.read",
                effect: PermissionEffect.Allow));

            Assert.Equal(
                expected: "Identity.User.EmptyId",
                actual: emptyUserException.Code);
            Assert.Equal(
                expected: "userId",
                actual: emptyUserException.PropertyName);

            DomainException emptyPermissionException = Assert.Throws<DomainException>(() => new UserPermissionOverride(
                userId: Guid.Parse("80000000-0000-0000-0000-000000000010"),
                permissionKey: "   ",
                effect: PermissionEffect.Deny));

            Assert.Equal(
                expected: "Identity.Permission.Key.Empty",
                actual: emptyPermissionException.Code);
            Assert.Equal(
                expected: "permissionKey",
                actual: emptyPermissionException.PropertyName);

            DomainException longPermissionException = Assert.Throws<DomainException>(() => new UserPermissionOverride(
                userId: Guid.Parse("80000000-0000-0000-0000-000000000010"),
                permissionKey: new string(
                    c: 'p',
                    count: UserPermissionOverride.PermissionKeyMaxLength + 1),
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
            var permissionOverride = new UserPermissionOverride(
                userId: Guid.Parse("80000000-0000-0000-0000-000000000010"),
                permissionKey: "identity.users.read",
                effect: PermissionEffect.Allow);

            permissionOverride.SetEffect(PermissionEffect.Deny);

            Assert.Equal(
                expected: PermissionEffect.Deny,
                actual: permissionOverride.Effect);
        }
    }
}
