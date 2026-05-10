using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Models;

public sealed class EnvironmentalModelTests
{
    [Fact]
    public void CitySystemSnapshot_WhenKindIsInvalid_Throws()
    {
        Assert.ThrowsAny<Exception>(
            () => new CitySystemSnapshot(
                kind: (CitySystemKind)999,
                loadIndex: 0.1m,
                serviceQualityIndex: 0.9m,
                backlogIndex: 0.1m,
                failureRiskIndex: 0.1m));
    }

    [Fact]
    public void CitySystemSnapshot_RoundsNormalizedMetrics()
    {
        var snapshot = new CitySystemSnapshot(
            kind: CitySystemKind.Drainage,
            loadIndex: 0.12345m,
            serviceQualityIndex: 0.87654m,
            backlogIndex: 0.22225m,
            failureRiskIndex: 0.33335m);

        Assert.Equal(0.1235m, snapshot.LoadIndex);
        Assert.Equal(0.8765m, snapshot.ServiceQualityIndex);
        Assert.Equal(0.2223m, snapshot.BacklogIndex);
        Assert.Equal(0.3334m, snapshot.FailureRiskIndex);
    }

    [Fact]
    public void CityWeatherPressureProfile_NeutralCreatesZeroedProfile()
    {
        CityWeatherPressureProfile profile = CityWeatherPressureProfile.Neutral();

        Assert.Equal(0m, profile.RainPressure);
        Assert.Equal(0m, profile.SnowPressure);
        Assert.Equal(0m, profile.StormPressure);
        Assert.Equal(0m, profile.FreezePressure);
        Assert.Equal(0m, profile.ThawRelief);
    }

    [Fact]
    public void CityResourceSupplySnapshot_NeutralCreatesStableDefaults()
    {
        CityResourceSupplySnapshot snapshot = CityResourceSupplySnapshot.Neutral(
            effectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc,
            effectiveTickId: 3);

        Assert.Equal(0m, snapshot.SupplyStressIndex);
        Assert.Equal(1m, snapshot.FuelStockLevelIndex);
        Assert.Equal(1m, snapshot.EmergencyWaterResupplyReadinessIndex);
        Assert.Equal(3, snapshot.EffectiveTickId);
        Assert.Equal(SimulationSystemsTestData.CreatedAtUtc, snapshot.EffectiveAtUtc);
    }

    [Fact]
    public void CityResourceSupplySnapshot_WhenTimestampIsNotUtc_Throws()
    {
        Assert.ThrowsAny<Exception>(
            () => CityResourceSupplySnapshot.Neutral(
                effectiveAtUtc: new DateTimeOffset(2051, 2, 3, 8, 0, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void CityOperationalBudgetPressureSnapshot_NeutralNormalizesTickAndUtc()
    {
        CityOperationalBudgetPressureSnapshot snapshot = CityOperationalBudgetPressureSnapshot.Neutral(
            effectiveAtUtc: new DateTimeOffset(2051, 2, 3, 8, 0, 0, TimeSpan.FromHours(3)),
            effectiveTickId: -4);

        Assert.Equal(0m, snapshot.PressureIndex);
        Assert.Equal(0, snapshot.EffectiveTickId);
        Assert.Equal(TimeSpan.Zero, snapshot.EffectiveAtUtc.Offset);
    }
}
