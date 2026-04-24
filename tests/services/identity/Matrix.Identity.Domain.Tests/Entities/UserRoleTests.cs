using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

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

        Assert.Equal(userId, userRole.UserId);
        Assert.Equal(roleId, userRole.RoleId);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new UserRole(
            userId: Guid.Empty,
            roleId: Guid.Parse("80000000-0000-0000-0000-000000000002")));

        Assert.Equal("Identity.User.EmptyId", exception.Code);
        Assert.Equal("userId", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WithEmptyRoleId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new UserRole(
            userId: Guid.Parse("80000000-0000-0000-0000-000000000001"),
            roleId: Guid.Empty));

        Assert.Equal("Identity.Role.EmptyId", exception.Code);
        Assert.Equal("roleId", exception.PropertyName);
    }
}
