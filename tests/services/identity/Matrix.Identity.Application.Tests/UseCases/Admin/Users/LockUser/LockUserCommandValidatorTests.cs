using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.LockUser
{
    public sealed class LockUserCommandValidatorTests
    {
        private readonly LockUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidUserId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new LockUserCommand(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyUserId_ReturnsExpectedError()
        {
            ValidationResult? result = _validator.Validate(new LockUserCommand(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
        }
    }
}
