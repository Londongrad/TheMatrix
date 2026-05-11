using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;

public sealed class SyncCityResourceSupplyCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new SyncCityResourceSupplyCommandValidator();

        var result = validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithOutOfRangeValues_ReturnsErrors()
    {
        var validator = new SyncCityResourceSupplyCommandValidator();

        var result = validator.Validate(new SyncCityResourceSupplyCommand(
            CityId: Guid.Empty,
            SupplyStressIndex: -0.1m,
            FuelStockLevelIndex: 1.2m,
            FuelResupplyReadinessIndex: 0.4m,
            FuelShortageRiskIndex: 0.4m,
            SparePartsStockLevelIndex: 0.4m,
            SparePartsResupplyReadinessIndex: 0.4m,
            SparePartsShortageRiskIndex: 0.4m,
            FiltersStockLevelIndex: 0.4m,
            FiltersResupplyReadinessIndex: 0.4m,
            FiltersShortageRiskIndex: 0.4m,
            EmergencyWaterStockLevelIndex: 0.4m,
            EmergencyWaterResupplyReadinessIndex: 0.4m,
            EmergencyWaterShortageRiskIndex: 1.2m,
            EffectiveTickId: -1,
            EffectiveAtUtc: new DateTimeOffset(2052, 3, 4, 8, 0, 0, TimeSpan.FromHours(3))));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "SupplyStressIndex");
        Assert.Contains(result.Errors, x => x.PropertyName == "FuelStockLevelIndex");
        Assert.Contains(result.Errors, x => x.PropertyName == "EmergencyWaterShortageRiskIndex");
        Assert.Contains(result.Errors, x => x.PropertyName == "EffectiveTickId");
        Assert.Contains(result.Errors, x => x.PropertyName == "EffectiveAtUtc");
    }

    private static SyncCityResourceSupplyCommand CreateCommand()
    {
        return new SyncCityResourceSupplyCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            SupplyStressIndex: 0.32m,
            FuelStockLevelIndex: 0.51m,
            FuelResupplyReadinessIndex: 0.61m,
            FuelShortageRiskIndex: 0.23m,
            SparePartsStockLevelIndex: 0.49m,
            SparePartsResupplyReadinessIndex: 0.58m,
            SparePartsShortageRiskIndex: 0.31m,
            FiltersStockLevelIndex: 0.44m,
            FiltersResupplyReadinessIndex: 0.57m,
            FiltersShortageRiskIndex: 0.28m,
            EmergencyWaterStockLevelIndex: 0.70m,
            EmergencyWaterResupplyReadinessIndex: 0.62m,
            EmergencyWaterShortageRiskIndex: 0.15m,
            EffectiveTickId: 5,
            EffectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc);
    }
}
