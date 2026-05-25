using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class UserRoleTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsProperties()
        {
            var userId = Guid.Parse("80000000-0000-0000-0000-000000000001");
            var roleId = Guid.Parse("80000000-0000-0000-0000-000000000002");

            var userRole = new UserRole(
                userId: userId,
                roleId: roleId);

            Assert.Equal(
                expected: userId,
                actual: userRole.UserId);
            Assert.Equal(
                expected: roleId,
                actual: userRole.RoleId);
        }

        [Fact]
        public void Constructor_WithEmptyUserId_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new UserRole(
                userId: Guid.Empty,
                roleId: Guid.Parse("80000000-0000-0000-0000-000000000002")));

            Assert.Equal(
                expected: "Identity.User.EmptyId",
                actual: exception.Code);
            Assert.Equal(
                expected: "userId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Constructor_WithEmptyRoleId_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new UserRole(
                userId: Guid.Parse("80000000-0000-0000-0000-000000000001"),
                roleId: Guid.Empty));

            Assert.Equal(
                expected: "Identity.Role.EmptyId",
                actual: exception.Code);
            Assert.Equal(
                expected: "roleId",
                actual: exception.PropertyName);
        }
    }
}
