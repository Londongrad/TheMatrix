using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails
{
    public sealed class GetCityResidentDetailsQueryValidatorTests
    {
        private readonly GetCityResidentDetailsQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentDetailsQuery(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    PersonId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyIds_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityResidentDetailsQuery(
                    CityId: Guid.Empty,
                    PersonId: Guid.Empty,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "PersonId");
        }
    }
}
