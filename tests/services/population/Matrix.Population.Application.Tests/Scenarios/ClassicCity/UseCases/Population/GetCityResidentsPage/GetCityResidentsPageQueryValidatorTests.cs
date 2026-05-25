using FluentValidation.Results;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed class GetCityResidentsPageQueryValidatorTests
    {
        private readonly GetCityResidentsPageQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentsPageQuery(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 3),
                    Pagination: new Pagination(
                        pageNumber: 1,
                        pageSize: 20)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentsPageQuery(
                    CityId: Guid.Empty,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 3),
                    Pagination: new Pagination(
                        pageNumber: 1,
                        pageSize: 20)));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
        }

        [Fact]
        public void Validate_WithNullPagination_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentsPageQuery(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 3),
                    Pagination: null!));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Pagination");
        }
    }
}
