using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GrantUserPermission
{
    public sealed class GrantUserPermissionCommandValidatorTests
    {
        private readonly GrantUserPermissionCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GrantUserPermissionCommand(
                    UserId: Guid.NewGuid(),
                    TargetPermissionKey: "users.read"));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidCommand_ReturnsExpectedErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GrantUserPermissionCommand(
                    UserId: Guid.Empty,
                    TargetPermissionKey: string.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "TargetPermissionKey" &&
                             x.ErrorMessage == "Permission key must not be empty");
        }
    }
}
