using FluentValidation.Results;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions
{
    public sealed class AdvanceCityEnvironmentalConditionsCommandValidatorTests
    {
        private readonly AdvanceCityEnvironmentalConditionsCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new AdvanceCityEnvironmentalConditionsCommand(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 4));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new AdvanceCityEnvironmentalConditionsCommand(
                    CityId: Guid.Empty,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2052,
                        month: 3,
                        day: 4,
                        hour: 7,
                        minute: 59,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    TickId: 4));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "FromSimTimeUtc");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ToSimTimeUtc");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.ErrorMessage.Contains("ToSimTimeUtc must be greater than FromSimTimeUtc."));
        }
    }
}
