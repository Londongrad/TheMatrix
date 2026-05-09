using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityStockpilePolicyTests
{
    [Fact]
    public void CreateSeed_RespectsDevelopmentProfiles()
    {
        var policy = new CityStockpilePolicy();

        CityStockpileSnapshot struggling = policy.CreateSeed("struggling", CreatedAtUtc);
        CityStockpileSnapshot affluent = policy.CreateSeed("affluent", CreatedAtUtc);

        Assert.True(affluent.Food.StockLevelIndex > struggling.Food.StockLevelIndex);
        Assert.True(affluent.Fuel.ResupplyReadinessIndex > struggling.Fuel.ResupplyReadinessIndex);
        Assert.True(struggling.SupplyStressIndex > affluent.SupplyStressIndex);
    }

    [Fact]
    public void Advance_WithNonPositiveElapsed_ReturnsSameSnapshot()
    {
        var policy = new CityStockpilePolicy();
        CityStockpileSnapshot current = CreateSnapshot();

        CityStockpileSnapshot advanced = policy.Advance(current, TimeSpan.Zero);

        Assert.Same(current, advanced);
    }

    [Fact]
    public void Advance_WithElapsedTime_UpdatesStockAndEvaluationTime()
    {
        var policy = new CityStockpilePolicy();
        CityStockpileSnapshot current = CreateSnapshot();

        CityStockpileSnapshot advanced = policy.Advance(current, TimeSpan.FromDays(1));

        Assert.Equal(CreatedAtUtc.AddDays(1), advanced.EvaluatedAtUtc);
        Assert.NotEqual(current.Fuel.StockLevelIndex, advanced.Fuel.StockLevelIndex);
        Assert.NotEqual(current.SupplyStressIndex, advanced.SupplyStressIndex);
    }

    [Fact]
    public void SetEmergencyRationing_UpdatesFlagAndStress()
    {
        var policy = new CityStockpilePolicy();
        CityStockpileSnapshot current = CreateSnapshot(emergencyRationingEnabled: false, supplyStressIndex: 0.40m);

        CityStockpileSnapshot updated = policy.SetEmergencyRationing(current, enabled: true);

        Assert.True(updated.EmergencyRationingEnabled);
        Assert.True(updated.SupplyStressIndex < current.SupplyStressIndex);
    }

    [Fact]
    public void DispatchResupply_FocusedResourceImprovesMoreThanUnfocused()
    {
        var policy = new CityStockpilePolicy();
        CityStockpileSnapshot current = CreateSnapshot();

        CityStockpileSnapshot updated = policy.DispatchResupply(current, ResupplyFocus.Medicine, ResupplyIntensity.High);

        decimal medicineGain = updated.Medicine.StockLevelIndex - current.Medicine.StockLevelIndex;
        decimal foodGain = updated.Food.StockLevelIndex - current.Food.StockLevelIndex;

        Assert.True(medicineGain > foodGain);
        Assert.True(updated.Medicine.ShortageRiskIndex < current.Medicine.ShortageRiskIndex);
    }

    [Fact]
    public void ApplySystemsDemand_FutureDemandIsNoOp_AndCurrentDemandMutatesOperationalLines()
    {
        var policy = new CityStockpilePolicy();
        CityStockpileSnapshot futureDemand = CreateSnapshot(evaluatedAtUtc: CreatedAtUtc);
        futureDemand = futureDemand with
        {
            SystemsDemand = CreateSystemsDemand(
                fuelDemandPressureIndex: 0.70m,
                sparePartsDemandPressureIndex: 0.60m,
                filtersDemandPressureIndex: 0.50m,
                emergencyWaterDemandPressureIndex: 0.40m,
                effectiveAtUtc: CreatedAtUtc.AddHours(2))
        };

        CityStockpileSnapshot noOp = policy.ApplySystemsDemand(futureDemand);

        Assert.Same(futureDemand, noOp);

        CityStockpileSnapshot activeDemand = futureDemand with
        {
            SystemsDemand = CreateSystemsDemand(
                fuelDemandPressureIndex: 0.70m,
                sparePartsDemandPressureIndex: 0.60m,
                filtersDemandPressureIndex: 0.50m,
                emergencyWaterDemandPressureIndex: 0.40m,
                effectiveAtUtc: CreatedAtUtc.AddHours(-1))
        };

        CityStockpileSnapshot applied = policy.ApplySystemsDemand(activeDemand);

        Assert.True(applied.Fuel.DemandPressureIndex > activeDemand.Fuel.DemandPressureIndex);
        Assert.True(applied.SpareParts.DemandPressureIndex > activeDemand.SpareParts.DemandPressureIndex);
        Assert.Equal(activeDemand.Food, applied.Food);
    }
}
