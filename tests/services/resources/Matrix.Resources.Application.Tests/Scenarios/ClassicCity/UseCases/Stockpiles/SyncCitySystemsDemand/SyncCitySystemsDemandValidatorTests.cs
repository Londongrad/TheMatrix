using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Xunit;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed class SyncCitySystemsDemandValidatorTests
    {
        [Fact]
        public void Validator_RejectsInvalidIdentifiersAndDemandValues()
        {
            var validator = new SyncCitySystemsDemandCommandValidator();

            ValidationResult? result = validator.Validate(
                new SyncCitySystemsDemandCommand(
                    CityId: Guid.Empty,
                    FuelDemandPressureIndex: -0.1m,
                    SparePartsDemandPressureIndex: 0.2m,
                    FiltersDemandPressureIndex: 1.4m,
                    EmergencyWaterDemandPressureIndex: 0.3m,
                    OverallDemandPressureIndex: 1.1m,
                    EffectiveTickId: -1,
                    EffectiveAtUtc: new DateTimeOffset(
                        year: 2049,
                        month: 1,
                        day: 1,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(9))));

            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 5);
        }
    }
}
