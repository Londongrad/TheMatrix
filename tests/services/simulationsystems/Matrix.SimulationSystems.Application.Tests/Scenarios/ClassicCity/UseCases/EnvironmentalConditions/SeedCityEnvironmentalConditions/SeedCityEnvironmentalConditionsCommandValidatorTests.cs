using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions
{
    public sealed class SeedCityEnvironmentalConditionsCommandValidatorTests
    {
        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            var validator = new SeedCityEnvironmentalConditionsCommandValidator();

            ValidationResult? result = validator.Validate(
                new SeedCityEnvironmentalConditionsCommand(
                    CityId: Guid.Parse("cccccccc-dddd-eeee-ffff-aaaaaaaaaaaa"),
                    CreatedAtUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    DevelopmentLevel: "standard"));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            var validator = new SeedCityEnvironmentalConditionsCommandValidator();

            ValidationResult? result = validator.Validate(
                new SeedCityEnvironmentalConditionsCommand(
                    CityId: Guid.Empty,
                    CreatedAtUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    DevelopmentLevel: "standard"));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CreatedAtUtc");
        }
    }
}
