using Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserDetails;

public sealed class GetUserDetailsQueryValidatorTests
{
    private readonly GetUserDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetUserDetailsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsError()
    {
        var result = _validator.Validate(new GetUserDetailsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "UserId");
    }
}
