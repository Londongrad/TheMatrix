using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserRoles
{
    public sealed class GetUserRolesQueryValidatorTests
    {
        private readonly GetUserRolesQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidUserId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new GetUserRolesQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyUserId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetUserRolesQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId");
        }
    }
}
