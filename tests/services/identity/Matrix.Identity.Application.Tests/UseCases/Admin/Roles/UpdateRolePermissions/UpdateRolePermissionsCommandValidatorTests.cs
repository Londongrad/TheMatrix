using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.UpdateRolePermissions
{
    public sealed class UpdateRolePermissionsCommandValidatorTests
    {
        private readonly UpdateRolePermissionsCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidRoleId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateRolePermissionsCommand(
                    RoleId: Guid.NewGuid(),
                    RolePermissionKeys: Array.Empty<string>()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyRoleId_ReturnsExpectedError()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateRolePermissionsCommand(
                    RoleId: Guid.Empty,
                    RolePermissionKeys: Array.Empty<string>()));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "RoleId" && x.ErrorMessage == "RoleId must not be empty");
        }
    }
}
