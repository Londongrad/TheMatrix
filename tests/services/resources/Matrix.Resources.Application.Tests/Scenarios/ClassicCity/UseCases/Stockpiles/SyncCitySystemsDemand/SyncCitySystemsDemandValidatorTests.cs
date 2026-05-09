using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;

public sealed class SyncCitySystemsDemandValidatorTests
{
    [Fact]
    public void Validator_RejectsInvalidIdentifiersAndDemandValues()
    {
        var validator = new SyncCitySystemsDemandCommandValidator();

        var result = validator.Validate(new SyncCitySystemsDemandCommand(
            CityId: Guid.Empty,
            FuelDemandPressureIndex: -0.1m,
            SparePartsDemandPressureIndex: 0.2m,
            FiltersDemandPressureIndex: 1.4m,
            EmergencyWaterDemandPressureIndex: 0.3m,
            OverallDemandPressureIndex: 1.1m,
            EffectiveTickId: -1,
            EffectiveAtUtc: new DateTimeOffset(2049, 1, 1, 18, 0, 0, TimeSpan.FromHours(9))));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5);
    }
}
