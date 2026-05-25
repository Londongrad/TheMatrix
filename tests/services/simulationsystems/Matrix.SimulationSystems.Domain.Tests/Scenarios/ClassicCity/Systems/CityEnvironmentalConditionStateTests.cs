using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Systems
{
    public sealed class CityEnvironmentalConditionStateTests
    {
        [Fact]
        public void Create_UsesSeedAndInitializesPendingWorkAndTick()
        {
            CityEnvironmentalConditionSnapshot seed = SimulationSystemsTestData.CreateSeed();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();

            Assert.Equal(
                expected: SimulationSystemsTestData.CreateHostId(),
                actual: state.SimulationHostId);
            Assert.Equal(
                expected: seed.EvaluatedAtUtc,
                actual: state.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: 0,
                actual: state.LastAppliedTickId);
            Assert.False(state.PendingDrainageMaintenance.IsScheduled);
            Assert.Equal(
                expected: seed.Drainage.LoadIndex,
                actual: state.Drainage.LoadIndex);
            Assert.Equal(
                expected: 0m,
                actual: state.WeatherPressure.RainPressure);
        }

        [Fact]
        public void ApplyWeatherPressure_ReplacesProfile()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            var profile = new CityWeatherPressureProfile(
                rainPressure: 0.4m,
                snowPressure: 0.1m,
                stormPressure: 0.5m,
                freezePressure: 0.3m,
                thawRelief: 0.2m);

            state.ApplyWeatherPressure(profile);

            Assert.Equal(
                expected: 0.4m,
                actual: state.WeatherPressure.RainPressure);
            Assert.Equal(
                expected: 0.5m,
                actual: state.WeatherPressure.StormPressure);
        }

        [Fact]
        public void ApplySnapshot_WhenSnapshotMovesBackward_Throws()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            CityEnvironmentalConditionSnapshot older = SimulationSystemsTestData.CreateSeed(
                evaluatedAtUtc: SimulationSystemsTestData.CreatedAtUtc.AddMinutes(-1));

            Assert.ThrowsAny<Exception>(() => state.ApplySnapshot(older));
        }

        [Fact]
        public void ApplySnapshot_UpdatesIndicesAndSnapshotState()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            CityEnvironmentalConditionSnapshot updated = SimulationSystemsTestData.CreateUpdatedSnapshot(
                baseline: state.ToSnapshot(),
                evaluatedAtUtc: SimulationSystemsTestData.LaterUtc);

            state.ApplySnapshot(updated);

            Assert.Equal(
                expected: 0.42m,
                actual: state.FloodingIndex.Value);
            Assert.Equal(
                expected: 0.79m,
                actual: state.UtilityContinuityIndex.Value);
            Assert.Equal(
                expected: 4,
                actual: state.ResourceSupply.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsTestData.LaterUtc,
                actual: state.LastEvaluatedAtUtc);
        }

        [Fact]
        public void ToSnapshot_RoundTripsCurrentState()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            state.ApplyWeatherPressure(
                new CityWeatherPressureProfile(
                    rainPressure: 0.2m,
                    snowPressure: 0.3m,
                    stormPressure: 0.4m,
                    freezePressure: 0.5m,
                    thawRelief: 0.1m));
            state.MarkTickApplied(6);

            CityEnvironmentalConditionSnapshot snapshot = state.ToSnapshot();

            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: snapshot.EvaluatedAtUtc);
            Assert.Equal(
                expected: state.Drainage.Kind,
                actual: snapshot.Drainage.Kind);
            Assert.Equal(
                expected: state.PowerCoverageIndex.Value,
                actual: snapshot.PowerCoverageIndex.Value);
            Assert.Equal(
                expected: state.UtilityContinuityIndex.Value,
                actual: snapshot.UtilityContinuityIndex.Value);
        }

        [Fact]
        public void MarkTickApplied_WhenTickMovesBackward_Throws()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            state.MarkTickApplied(5);

            Assert.Throws<InvalidOperationException>(() => state.MarkTickApplied(4));
        }

        [Fact]
        public void MarkTickApplied_UpdatesLastAppliedTickId()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();

            state.MarkTickApplied(7);

            Assert.Equal(
                expected: 7,
                actual: state.LastAppliedTickId);
        }

        [Fact]
        public void ApplyResourceSupplyAndBudgetPressure_UpdateEmbeddedStates()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            var supply = CityResourceSupplySnapshot.Neutral(
                effectiveAtUtc: SimulationSystemsTestData.LaterUtc,
                effectiveTickId: 9);
            var pressure = CityOperationalBudgetPressureSnapshot.Neutral(
                effectiveAtUtc: SimulationSystemsTestData.LaterUtc,
                effectiveTickId: 11);

            state.ApplyResourceSupply(supply);
            state.ApplyOperationalBudgetPressure(pressure);

            Assert.Equal(
                expected: 9,
                actual: state.ResourceSupply.EffectiveTickId);
            Assert.Equal(
                expected: 11,
                actual: state.OperationalBudgetPressure.EffectiveTickId);
            Assert.Equal(
                expected: SimulationSystemsTestData.LaterUtc,
                actual: state.ResourceSupply.EffectiveAtUtc);
        }

        [Fact]
        public void ScheduleAndApplyDuePendingOperations_DispatchesAndClearsWork()
        {
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();

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
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            state.ScheduleSnowRemovalMaintenance(
                focus: SnowRemovalMaintenanceFocus.RouteClearance,
                intensity: SnowRemovalMaintenanceIntensity.Standard,
                readyAtTickId: 8);

            bool applied = state.ApplyDuePendingOperations(7);

            Assert.False(applied);
            Assert.True(state.PendingSnowRemovalMaintenance.IsScheduled);
        }
    }
}
