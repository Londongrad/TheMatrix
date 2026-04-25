using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRoleMembersPage;

public sealed class GetRoleMembersPageQueryValidatorTests
{
    private readonly GetRoleMembersPageQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetRoleMembersPageQuery(
            Guid.NewGuid(),
            new Pagination(pageNumber: 1, pageSize: 20)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyRoleId_ReturnsError()
    {
        var result = _validator.Validate(new GetRoleMembersPageQuery(
            Guid.Empty,
            new Pagination(pageNumber: 1, pageSize: 20)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "RoleId");
    }

    [Fact]
    public void Validate_WithNullPagination_ReturnsError()
    {
        var result = _validator.Validate(new GetRoleMembersPageQuery(Guid.NewGuid(), null!));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Pagination");
    }
}
