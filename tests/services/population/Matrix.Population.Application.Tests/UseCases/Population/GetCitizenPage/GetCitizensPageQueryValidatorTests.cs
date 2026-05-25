using FluentValidation.Results;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Xunit;

namespace Matrix.Population.Application.Tests.UseCases.Population.GetCitizenPage
{
    public sealed class GetCitizensPageQueryValidatorTests
    {
        private readonly GetCitizensPageQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidPagination_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetCitizensPageQuery(
                    new Pagination(
                        pageNumber: 1,
                        pageSize: 50)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithNullPagination_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetCitizensPageQuery(Pagination: null!));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Pagination");
        }
    }
}
