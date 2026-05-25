using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserPermissions
{
    public sealed class UpdateUserPermissionsCommandValidatorTests
    {
        private readonly UpdateUserPermissionsCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidOverrides_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateUserPermissionsCommand(
                    UserId: Guid.NewGuid(),
                    Overrides:
                    [
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: "users.read",
                            Effect: "Allow")
                    ]));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidOverrides_ReturnsExpectedErrors()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateUserPermissionsCommand(
                    UserId: Guid.Empty,
                    Overrides:
                    [
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: string.Empty,
                            Effect: "Maybe"),
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: "users.read",
                            Effect: "Deny"),
                        new UpdateUserPermissionOverrideInput(
                            PermissionKey: " users.read ",
                            Effect: "Allow")
                    ]));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Overrides[0].PermissionKey" &&
                             x.ErrorMessage == "PermissionKey must not be empty");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Overrides[0].Effect" &&
                             x.ErrorMessage == "Effect must be Allow or Deny");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Overrides" && x.ErrorMessage == "Permission keys must be unique");
        }
    }
}
