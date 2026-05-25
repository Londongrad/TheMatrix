using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.Common
{
    public sealed class CityPopulationEnvironmentInputValidatorTests
    {
        private readonly CityPopulationEnvironmentInputValidator _validator = new();

        [Fact]
        public void Validate_WithValidInput_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new CityPopulationEnvironmentInput(
                    ClimateZone: "Temperate",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 180));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyNamesAndInvalidOffset_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new CityPopulationEnvironmentInput(
                    ClimateZone: "",
                    Hemisphere: "",
                    UtcOffsetMinutes: (14 * 60) + 1));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ClimateZone");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Hemisphere");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UtcOffsetMinutes");
        }
    }
}
