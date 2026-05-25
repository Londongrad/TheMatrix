using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UnlockUser
{
    public sealed class UnlockUserCommandValidatorTests
    {
        private readonly UnlockUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidUserId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new UnlockUserCommand(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyUserId_ReturnsExpectedError()
        {
            ValidationResult? result = _validator.Validate(new UnlockUserCommand(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId" && x.ErrorMessage == "UserId must not be empty");
        }
    }
}
