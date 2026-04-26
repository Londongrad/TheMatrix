using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.LockUser;

public sealed class LockUserCommandValidatorTests
{
    private readonly LockUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new LockUserCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyUserId_ReturnsExpectedError()
    {
        var result = _validator.Validate(new LockUserCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
    }
}
