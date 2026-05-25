using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandValidatorTests
    {
        private readonly AdvanceCityPopulationCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new AdvanceCityPopulationCommand(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 5,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 5,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 15));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidInputs_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new AdvanceCityPopulationCommand(
                    CityId: Guid.Empty,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 5,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3)),
                    TickId: -1));

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
                filter: x => x.PropertyName == "TickId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.ErrorMessage.Contains("ToSimTimeUtc date cannot be earlier than FromSimTimeUtc date."));
        }
    }
}
