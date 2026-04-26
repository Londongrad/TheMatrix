using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserPermissions;

public sealed class GetUserPermissionsQueryValidatorTests
{
    private readonly GetUserPermissionsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetUserPermissionsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsError()
    {
        var result = _validator.Validate(new GetUserPermissionsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "UserId");
    }
}
