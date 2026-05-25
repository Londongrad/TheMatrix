using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandValidatorTests
    {
        private readonly UpdateUserRolesCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidUserId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateUserRolesCommand(
                    UserId: Guid.NewGuid(),
                    RoleIds: Array.Empty<Guid>()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyUserId_ReturnsExpectedError()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateUserRolesCommand(
                    UserId: Guid.Empty,
                    RoleIds: Array.Empty<Guid>()));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
        }
    }
}
