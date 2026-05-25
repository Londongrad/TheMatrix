using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.DeleteRole
{
    public sealed class DeleteRoleCommandValidatorTests
    {
        private readonly DeleteRoleCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidRoleId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new DeleteRoleCommand(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyRoleId_ReturnsExpectedError()
        {
            ValidationResult? result = _validator.Validate(new DeleteRoleCommand(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "RoleId" && x.ErrorMessage == "RoleId must not be empty.");
        }
    }
}
