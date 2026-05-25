using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.RenameRole
{
    public sealed class RenameRoleCommandValidatorTests
    {
        private readonly RenameRoleCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new RenameRoleCommand(
                    RoleId: Guid.NewGuid(),
                    Name: "Operators"));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyRoleId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new RenameRoleCommand(
                    RoleId: Guid.Empty,
                    Name: "Operators"));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "RoleId" && x.ErrorMessage == "RoleId must not be empty");
        }

        [Fact]
        public void Validate_WithEmptyName_ReturnsNameError()
        {
            ValidationResult? result = _validator.Validate(
                new RenameRoleCommand(
                    RoleId: Guid.NewGuid(),
                    Name: string.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Name");
        }
    }
}
