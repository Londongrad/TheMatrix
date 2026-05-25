using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions
{
    public sealed class UpdateDefaultUserAccessPermissionsCommandValidatorTests
    {
        private readonly UpdateDefaultUserAccessPermissionsCommandValidator _validator = new();

        [Fact]
        public void Validate_WithPermissionKeys_ReturnsNoErrors()
        {
            ValidationResult? result =
                _validator.Validate(new UpdateDefaultUserAccessPermissionsCommand(["users.read"]));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithNullPermissionKeys_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new UpdateDefaultUserAccessPermissionsCommand(null!));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "PermissionKeys");
        }
    }
}
