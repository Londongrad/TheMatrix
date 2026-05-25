using FluentValidation.Results;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRoleMembersPage
{
    public sealed class GetRoleMembersPageQueryValidatorTests
    {
        private readonly GetRoleMembersPageQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetRoleMembersPageQuery(
                    RoleId: Guid.NewGuid(),
                    Pagination: new Pagination(
                        pageNumber: 1,
                        pageSize: 20)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyRoleId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new GetRoleMembersPageQuery(
                    RoleId: Guid.Empty,
                    Pagination: new Pagination(
                        pageNumber: 1,
                        pageSize: 20)));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "RoleId");
        }

        [Fact]
        public void Validate_WithNullPagination_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new GetRoleMembersPageQuery(
                    RoleId: Guid.NewGuid(),
                    Pagination: null!));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Pagination");
        }
    }
}
