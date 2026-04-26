using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUsersPage;

public sealed class GetUsersPageQueryValidatorTests
{
    private readonly GetUsersPageQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidPagination_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetUsersPageQuery(new Pagination(pageNumber: 1, pageSize: 20)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithNullPagination_ReturnsError()
    {
        var result = _validator.Validate(new GetUsersPageQuery(null!));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Pagination");
    }
}
