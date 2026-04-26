using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserPermissions;

public sealed class UpdateUserPermissionsCommandValidatorTests
{
    private readonly UpdateUserPermissionsCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidOverrides_ReturnsNoErrors()
    {
        var result = _validator.Validate(new UpdateUserPermissionsCommand(
            Guid.NewGuid(),
            [new UpdateUserPermissionOverrideInput("users.read", "Allow")]));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidOverrides_ReturnsExpectedErrors()
    {
        var result = _validator.Validate(new UpdateUserPermissionsCommand(
            Guid.Empty,
            [
                new UpdateUserPermissionOverrideInput(string.Empty, "Maybe"),
                new UpdateUserPermissionOverrideInput("users.read", "Deny"),
                new UpdateUserPermissionOverrideInput(" users.read ", "Allow")
            ]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
        Assert.Contains(result.Errors, x => x.PropertyName == "Overrides[0].PermissionKey" && x.ErrorMessage == "PermissionKey must not be empty");
        Assert.Contains(result.Errors, x => x.PropertyName == "Overrides[0].Effect" && x.ErrorMessage == "Effect must be Allow or Deny");
        Assert.Contains(result.Errors, x => x.PropertyName == "Overrides" && x.ErrorMessage == "Permission keys must be unique");
    }
}
