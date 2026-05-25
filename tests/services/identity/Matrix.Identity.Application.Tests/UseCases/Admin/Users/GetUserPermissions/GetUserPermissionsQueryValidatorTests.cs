using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserPermissions
{
    public sealed class GetUserPermissionsQueryValidatorTests
    {
        private readonly GetUserPermissionsQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidUserId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new GetUserPermissionsQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyUserId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetUserPermissionsQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserId");
        }
    }
}
