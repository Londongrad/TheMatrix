using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions
{
    public sealed class RecalculateCityEnvironmentalConditionsCommandValidatorTests
    {
        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            var validator = new RecalculateCityEnvironmentalConditionsCommandValidator();

            ValidationResult? result = validator.Validate(
                new RecalculateCityEnvironmentalConditionsCommand(
                    CityId: Guid.Parse("dddddddd-eeee-ffff-aaaa-bbbbbbbbbbbb"),
                    AtSimTimeUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    Weather: SimulationSystemsApplicationTestSupport.CreateWeather()));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            var validator = new RecalculateCityEnvironmentalConditionsCommandValidator();

            ValidationResult? result = validator.Validate(
                new RecalculateCityEnvironmentalConditionsCommand(
                    CityId: Guid.Empty,
                    AtSimTimeUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    Weather: null!));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "AtSimTimeUtc");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Weather");
        }
    }
}
