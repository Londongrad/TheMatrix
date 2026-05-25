using FluentValidation.Results;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.UpdateCityEnvironment
{
    public sealed class UpdateCityEnvironmentCommandValidatorTests
    {
        private readonly UpdateCityEnvironmentCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidValues_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateCityEnvironmentCommand(
                    CityId: Guid.NewGuid(),
                    ClimateZone: "Temperate",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 180));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidValues_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new UpdateCityEnvironmentCommand(
                    CityId: Guid.Empty,
                    ClimateZone: "Mars",
                    Hemisphere: "Up",
                    UtcOffsetMinutes: 17));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "ClimateZone");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "Hemisphere");
            Assert.Contains(
                collection: result.Errors,
                filter: error => error.PropertyName == "UtcOffsetMinutes");
        }
    }
}
