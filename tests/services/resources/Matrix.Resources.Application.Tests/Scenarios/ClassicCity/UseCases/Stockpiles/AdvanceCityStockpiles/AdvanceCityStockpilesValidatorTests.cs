using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Xunit;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles
{
    public sealed class AdvanceCityStockpilesValidatorTests
    {
        [Fact]
        public void Validator_RejectsEmptyCityIdAndNonUtcTimestamps()
        {
            var validator = new AdvanceCityStockpilesCommandValidator();

            ValidationResult? result = validator.Validate(
                new AdvanceCityStockpilesCommand(
                    CityId: Guid.Empty,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2049,
                        month: 1,
                        day: 1,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(9)),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2049,
                        month: 1,
                        day: 1,
                        hour: 19,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(9)),
                    TickId: 4));

            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 3);
        }
    }
}
