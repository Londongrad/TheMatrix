using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserRoles;

public sealed class GetUserRolesQueryValidatorTests
{
    private readonly GetUserRolesQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetUserRolesQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsError()
    {
        var result = _validator.Validate(new GetUserRolesQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "UserId");
    }
}
