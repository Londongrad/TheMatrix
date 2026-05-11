using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Systems;

public sealed class CityEnvironmentalConditionStateTests
{
    [Fact]
    public void Create_UsesSeedAndInitializesPendingWorkAndTick()
    {
        CityEnvironmentalConditionSnapshot seed = SimulationSystemsTestData.CreateSeed();
        var state = SimulationSystemsTestData.CreateState();

        Assert.Equal(SimulationSystemsTestData.CreateHostId(), state.SimulationHostId);
        Assert.Equal(seed.EvaluatedAtUtc, state.LastEvaluatedAtUtc);
        Assert.Equal(0, state.LastAppliedTickId);
        Assert.False(state.PendingDrainageMaintenance.IsScheduled);
        Assert.Equal(seed.Drainage.LoadIndex, state.Drainage.LoadIndex);
        Assert.Equal(0m, state.WeatherPressure.RainPressure);
    }

    [Fact]
    public void ApplyWeatherPressure_ReplacesProfile()
    {
        var state = SimulationSystemsTestData.CreateState();
        var profile = new CityWeatherPressureProfile(
            rainPressure: 0.4m,
            snowPressure: 0.1m,
            stormPressure: 0.5m,
            freezePressure: 0.3m,
            thawRelief: 0.2m);

        state.ApplyWeatherPressure(profile);

        Assert.Equal(0.4m, state.WeatherPressure.RainPressure);
        Assert.Equal(0.5m, state.WeatherPressure.StormPressure);
    }

    [Fact]
    public void ApplySnapshot_WhenSnapshotMovesBackward_Throws()
    {
        var state = SimulationSystemsTestData.CreateState();
        CityEnvironmentalConditionSnapshot older = SimulationSystemsTestData.CreateSeed(
            evaluatedAtUtc: SimulationSystemsTestData.CreatedAtUtc.AddMinutes(-1));

        Assert.ThrowsAny<Exception>(() => state.ApplySnapshot(older));
    }

    [Fact]
    public void ApplySnapshot_UpdatesIndicesAndSnapshotState()
    {
        var state = SimulationSystemsTestData.CreateState();
        CityEnvironmentalConditionSnapshot updated = SimulationSystemsTestData.CreateUpdatedSnapshot(
            baseline: state.ToSnapshot(),
            evaluatedAtUtc: SimulationSystemsTestData.LaterUtc);

        state.ApplySnapshot(updated);

        Assert.Equal(0.42m, state.FloodingIndex.Value);
        Assert.Equal(0.79m, state.UtilityContinuityIndex.Value);
        Assert.Equal(4, state.ResourceSupply.EffectiveTickId);
        Assert.Equal(SimulationSystemsTestData.LaterUtc, state.LastEvaluatedAtUtc);
    }

    [Fact]
    public void ToSnapshot_RoundTripsCurrentState()
    {
        var state = SimulationSystemsTestData.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(0.2m, 0.3m, 0.4m, 0.5m, 0.1m));
        state.MarkTickApplied(6);

        CityEnvironmentalConditionSnapshot snapshot = state.ToSnapshot();

        Assert.Equal(state.LastEvaluatedAtUtc, snapshot.EvaluatedAtUtc);
        Assert.Equal(state.Drainage.Kind, snapshot.Drainage.Kind);
        Assert.Equal(state.PowerCoverageIndex.Value, snapshot.PowerCoverageIndex.Value);
        Assert.Equal(state.UtilityContinuityIndex.Value, snapshot.UtilityContinuityIndex.Value);
    }

    [Fact]
    public void MarkTickApplied_WhenTickMovesBackward_Throws()
    {
        var state = SimulationSystemsTestData.CreateState();
        state.MarkTickApplied(5);

        Assert.Throws<InvalidOperationException>(() => state.MarkTickApplied(4));
    }

    [Fact]
    public void MarkTickApplied_UpdatesLastAppliedTickId()
    {
        var state = SimulationSystemsTestData.CreateState();

        state.MarkTickApplied(7);

        Assert.Equal(7, state.LastAppliedTickId);
    }

    [Fact]
    public void ApplyResourceSupplyAndBudgetPressure_UpdateEmbeddedStates()
    {
        var state = SimulationSystemsTestData.CreateState();
        var supply = CityResourceSupplySnapshot.Neutral(
            effectiveAtUtc: SimulationSystemsTestData.LaterUtc,
            effectiveTickId: 9);
        var pressure = CityOperationalBudgetPressureSnapshot.Neutral(
            effectiveAtUtc: SimulationSystemsTestData.LaterUtc,
            effectiveTickId: 11);

        state.ApplyResourceSupply(supply);
        state.ApplyOperationalBudgetPressure(pressure);

        Assert.Equal(9, state.ResourceSupply.EffectiveTickId);
        Assert.Equal(11, state.OperationalBudgetPressure.EffectiveTickId);
        Assert.Equal(SimulationSystemsTestData.LaterUtc, state.ResourceSupply.EffectiveAtUtc);
    }

    [Fact]
    public void ScheduleAndApplyDuePendingOperations_DispatchesAndClearsWork()
    {
        var state = SimulationSystemsTestData.CreateState();

        state.ScheduleDrainageMaintenance(
            focus: DrainageMaintenanceFocus.PumpRepairs,
            intensity: DrainageMaintenanceIntensity.Heavy,
            readyAtTickId: 4);
        state.ScheduleUtilityIncidentResponse(
            focus: UtilityIncidentResponseFocus.PowerOutages,
            intensity: UtilityIncidentResponseIntensity.Standard,
            focusDistrictId: Guid.Parse("73000000-0000-0000-0000-0000000000bb"),
            readyAtTickId: 4);

        bool applied = state.ApplyDuePendingOperations(4);

        Assert.True(applied);
        Assert.False(state.PendingDrainageMaintenance.IsScheduled);
        Assert.False(state.PendingUtilityIncidentResponse.IsScheduled);
        Assert.True(state.DrainageInfrastructure.PumpCapacityIndex > 0m);
        Assert.True(state.UtilityIncidentInfrastructure.DispatchReadinessIndex > 0m);
    }

    [Fact]
    public void ApplyDuePendingOperations_WhenNothingIsReady_ReturnsFalse()
    {
        var state = SimulationSystemsTestData.CreateState();
        state.ScheduleSnowRemovalMaintenance(
            focus: SnowRemovalMaintenanceFocus.RouteClearance,
            intensity: SnowRemovalMaintenanceIntensity.Standard,
            readyAtTickId: 8);

        bool applied = state.ApplyDuePendingOperations(7);

        Assert.False(applied);
        Assert.True(state.PendingSnowRemovalMaintenance.IsScheduled);
    }
}
