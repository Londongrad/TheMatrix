using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GrantUserPermission;

public sealed class GrantUserPermissionCommandValidatorTests
{
    private readonly GrantUserPermissionCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GrantUserPermissionCommand(Guid.NewGuid(), "users.read"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidCommand_ReturnsExpectedErrors()
    {
        var result = _validator.Validate(new GrantUserPermissionCommand(Guid.Empty, string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
        Assert.Contains(result.Errors, x => x.PropertyName == "TargetPermissionKey" && x.ErrorMessage == "Permission key must not be empty");
    }
}
