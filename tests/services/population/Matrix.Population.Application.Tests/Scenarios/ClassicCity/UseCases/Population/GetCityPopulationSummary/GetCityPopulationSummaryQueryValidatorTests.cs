using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary
{
    public sealed class GetCityPopulationSummaryQueryValidatorTests
    {
        private readonly GetCityPopulationSummaryQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetCityPopulationSummaryQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetCityPopulationSummaryQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
        }
    }
}
