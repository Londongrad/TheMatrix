using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetSuggestedCityNames
{
    public sealed class GetSuggestedCityNamesQueryValidatorTests
    {
        private readonly GetSuggestedCityNamesQueryValidator _validator = new();

        [Fact]
        public void Validate_WithCountWithinAllowedRange_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new GetSuggestedCityNamesQuery(
                    Seed: "alpha",
                    Count: 12));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(26)]
        public void Validate_WithCountOutsideAllowedRange_ReturnsError(int count)
        {
            ValidationResult? result = _validator.Validate(
                new GetSuggestedCityNamesQuery(
                    Seed: "alpha",
                    Count: count));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "Count");
        }
    }
}
