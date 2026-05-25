using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class RolePermissionTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsProperties()
        {
            var roleId = Guid.Parse("60000000-0000-0000-0000-000000000001");
            var rolePermission = new RolePermission(
                roleId: roleId,
                permissionKey: "identity.users.read");

            Assert.Equal(
                expected: roleId,
                actual: rolePermission.RoleId);
            Assert.Equal(
                expected: "identity.users.read",
                actual: rolePermission.PermissionKey);
        }

        [Fact]
        public void Constructor_WithEmptyRoleId_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new RolePermission(
                roleId: Guid.Empty,
                permissionKey: "identity.users.read"));

            Assert.Equal(
                expected: "Identity.Role.EmptyId",
                actual: exception.Code);
            Assert.Equal(
                expected: "roleId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WithWhitespacePermissionKey_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new RolePermission(
                roleId: Guid.Parse("60000000-0000-0000-0000-000000000001"),
                permissionKey: "   "));

            Assert.Equal(
                expected: "Identity.Permission.Key.Empty",
                actual: exception.Code);
            Assert.Equal(
                expected: "permissionKey",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WithTooLongPermissionKey_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new RolePermission(
                roleId: Guid.Parse("60000000-0000-0000-0000-000000000001"),
                permissionKey: new string(
                    c: 'p',
                    count: RolePermission.PermissionKeyMaxLength + 1)));

            Assert.Equal(
                expected: "Identity.Permission.Key.InvalidLength",
                actual: exception.Code);
            Assert.Equal(
                expected: "permissionKey",
                actual: exception.PropertyName);
        }
    }
}
