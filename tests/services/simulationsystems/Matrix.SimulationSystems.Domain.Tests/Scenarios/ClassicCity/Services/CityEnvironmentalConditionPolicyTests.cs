using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityEnvironmentalConditionPolicyTests
    {
        [Fact]
        public void CreateSeed_ForSameInputs_IsDeterministic()
        {
            var policy = new CityEnvironmentalConditionPolicy();

            CityEnvironmentalConditionSnapshot left = policy.CreateSeed(
                cityId: SimulationSystemsTestData.CityId,
                developmentLevel: "standard",
                asOfUtc: SimulationSystemsTestData.CreatedAtUtc);
            CityEnvironmentalConditionSnapshot right = policy.CreateSeed(
                cityId: SimulationSystemsTestData.CityId,
                developmentLevel: "standard",
                asOfUtc: SimulationSystemsTestData.CreatedAtUtc);

            Assert.Equal(
                expected: left.Drainage.LoadIndex,
                actual: right.Drainage.LoadIndex);
            Assert.Equal(
                expected: left.HeatingCoverageIndex.Value,
                actual: right.HeatingCoverageIndex.Value);
            Assert.Equal(
                expected: left.UtilityContinuityIndex.Value,
                actual: right.UtilityContinuityIndex.Value);
        }

        [Fact]
        public void CreateSeed_WhenTimestampIsNotUtc_Throws()
        {
            var policy = new CityEnvironmentalConditionPolicy();

            Assert.ThrowsAny<Exception>(() => policy.CreateSeed(
                cityId: SimulationSystemsTestData.CityId,
                developmentLevel: "standard",
                asOfUtc: new DateTimeOffset(
                    year: 2051,
                    month: 2,
                    day: 3,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3))));
        }

        [Fact]
        public void CreateSeed_StrugglingCityStartsWeakerThanAdvancedCity()
        {
            var policy = new CityEnvironmentalConditionPolicy();
            CityEnvironmentalConditionSnapshot struggling = policy.CreateSeed(
                cityId: SimulationSystemsTestData.CityId,
                developmentLevel: "struggling",
                asOfUtc: SimulationSystemsTestData.CreatedAtUtc);
            CityEnvironmentalConditionSnapshot advanced = policy.CreateSeed(
                cityId: Guid.Parse("73000000-0000-0000-0000-000000000002"),
                developmentLevel: "advanced",
                asOfUtc: SimulationSystemsTestData.CreatedAtUtc);

            Assert.True(
                struggling.DrainageInfrastructure.PumpCapacityIndex <
                advanced.DrainageInfrastructure.PumpCapacityIndex);
            Assert.True(
                struggling.HeatingInfrastructure.PlantCapacityIndex <
                advanced.HeatingInfrastructure.PlantCapacityIndex);
            Assert.True(struggling.RoadAccess.ServiceQualityIndex < advanced.RoadAccess.ServiceQualityIndex);
        }

        [Fact]
        public void Recalculate_WhenTimestampIsNotUtc_Throws()
        {
            CityEnvironmentalConditionPolicy policy = SimulationSystemsTestData.CreatePolicy();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();

            Assert.ThrowsAny<Exception>(() => policy.Recalculate(
                state: state,
                pressure: CreateHeavyPressure(),
                asOfUtc: new DateTimeOffset(
                    year: 2051,
                    month: 2,
                    day: 3,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.FromHours(3))));
        }

        [Fact]
        public void Recalculate_AppliesPressureAndPreservesOperationalSnapshots()
        {
            CityEnvironmentalConditionPolicy policy = SimulationSystemsTestData.CreatePolicy();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            var supply = new CityResourceSupplySnapshot(
                supplyStressIndex: 0.64m,
                fuelStockLevelIndex: 0.32m,
                fuelResupplyReadinessIndex: 0.41m,
                fuelShortageRiskIndex: 0.77m,
                sparePartsStockLevelIndex: 0.38m,
                sparePartsResupplyReadinessIndex: 0.45m,
                sparePartsShortageRiskIndex: 0.72m,
                filtersStockLevelIndex: 0.49m,
                filtersResupplyReadinessIndex: 0.53m,
                filtersShortageRiskIndex: 0.68m,
                emergencyWaterStockLevelIndex: 0.57m,
                emergencyWaterResupplyReadinessIndex: 0.61m,
                emergencyWaterShortageRiskIndex: 0.29m,
                effectiveTickId: 7,
                effectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(1));
            var budget = new CityOperationalBudgetPressureSnapshot(
                Balance: -250_000m,
                MunicipalOperationsExpenses: 500_000m,
                GeneralAvailableAmount: 80_000m,
                OperationsAvailableAmount: 55_000m,
                InfrastructureAvailableAmount: 40_000m,
                HealthcareAvailableAmount: 35_000m,
                GeneralAuthorizationLevel: "Restricted",
                OperationsAuthorizationLevel: "Emergency",
                InfrastructureAuthorizationLevel: "Restricted",
                HealthcareAuthorizationLevel: "Constrained",
                PressureIndex: 0.73m,
                EffectiveTickId: 8,
                EffectiveAtUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(2));

            state.ApplyResourceSupply(supply);
            state.ApplyOperationalBudgetPressure(budget);

            CityEnvironmentalConditionSnapshot snapshot = policy.Recalculate(
                state: state,
                pressure: CreateHeavyPressure(),
                asOfUtc: SimulationSystemsTestData.LaterUtc);

            Assert.Equal(
                expected: SimulationSystemsTestData.LaterUtc,
                actual: snapshot.EvaluatedAtUtc);
            Assert.Equal(
                expected: 7,
                actual: snapshot.ResourceSupply.EffectiveTickId);
            Assert.Equal(
                expected: 8,
                actual: snapshot.OperationalBudgetPressure.EffectiveTickId);
            Assert.Equal(
                expected: "Emergency",
                actual: snapshot.OperationalBudgetPressure.OperationsAuthorizationLevel);
            Assert.True(snapshot.FloodingIndex.Value > state.FloodingIndex.Value);
            Assert.True(snapshot.Drainage.LoadIndex > state.Drainage.LoadIndex);
        }

        [Fact]
        public void Advance_ForTenMinuteWindow_MatchesRecalculate()
        {
            CityEnvironmentalConditionPolicy policy = SimulationSystemsTestData.CreatePolicy();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            DateTimeOffset endUtc = SimulationSystemsTestData.CreatedAtUtc.AddMinutes(10);

            CityEnvironmentalConditionSnapshot recalculated = policy.Recalculate(
                state: state,
                pressure: CreateHeavyPressure(),
                asOfUtc: endUtc);
            CityEnvironmentalConditionSnapshot advanced = policy.Advance(
                state: state,
                pressure: CreateHeavyPressure(),
                fromUtc: SimulationSystemsTestData.CreatedAtUtc,
                toUtc: endUtc);

            Assert.Equal(
                expected: recalculated.EvaluatedAtUtc,
                actual: advanced.EvaluatedAtUtc);
            Assert.Equal(
                expected: recalculated.FloodingIndex.Value,
                actual: advanced.FloodingIndex.Value);
            Assert.Equal(
                expected: recalculated.RoadAccessibilityIndex.Value,
                actual: advanced.RoadAccessibilityIndex.Value);
            Assert.Equal(
                expected: recalculated.PowerCoverageIndex.Value,
                actual: advanced.PowerCoverageIndex.Value);
            Assert.Equal(
                expected: recalculated.UtilityIncidents.ServiceQualityIndex,
                actual: advanced.UtilityIncidents.ServiceQualityIndex);
        }

        [Fact]
        public void Advance_WhenWindowIsZero_ReturnsCurrentSnapshot()
        {
            CityEnvironmentalConditionPolicy policy = SimulationSystemsTestData.CreatePolicy();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();
            CityEnvironmentalConditionSnapshot baseline = state.ToSnapshot();

            CityEnvironmentalConditionSnapshot advanced = policy.Advance(
                state: state,
                pressure: CreateHeavyPressure(),
                fromUtc: SimulationSystemsTestData.CreatedAtUtc,
                toUtc: SimulationSystemsTestData.CreatedAtUtc);

            Assert.Equal(
                expected: baseline.EvaluatedAtUtc,
                actual: advanced.EvaluatedAtUtc);
            Assert.Equal(
                expected: baseline.FloodingIndex.Value,
                actual: advanced.FloodingIndex.Value);
            Assert.Equal(
                expected: baseline.SnowAccumulationIndex.Value,
                actual: advanced.SnowAccumulationIndex.Value);
            Assert.Equal(
                expected: baseline.RoadAccessibilityIndex.Value,
                actual: advanced.RoadAccessibilityIndex.Value);
            Assert.Equal(
                expected: baseline.UtilityContinuityIndex.Value,
                actual: advanced.UtilityContinuityIndex.Value);
        }

        [Fact]
        public void Advance_WhenWindowIsLonger_MovesFurtherTowardPressureTarget()
        {
            CityEnvironmentalConditionPolicy policy = SimulationSystemsTestData.CreatePolicy();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();

            CityEnvironmentalConditionSnapshot shortAdvance = policy.Advance(
                state: state,
                pressure: CreateHeavyPressure(),
                fromUtc: SimulationSystemsTestData.CreatedAtUtc,
                toUtc: SimulationSystemsTestData.CreatedAtUtc.AddMinutes(10));
            CityEnvironmentalConditionSnapshot longAdvance = policy.Advance(
                state: state,
                pressure: CreateHeavyPressure(),
                fromUtc: SimulationSystemsTestData.CreatedAtUtc,
                toUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(12));

            Assert.True(longAdvance.FloodingIndex.Value >= shortAdvance.FloodingIndex.Value);
            Assert.True(longAdvance.Drainage.LoadIndex >= shortAdvance.Drainage.LoadIndex);
        }

        [Fact]
        public void Advance_WhenWindowMovesBackward_Throws()
        {
            CityEnvironmentalConditionPolicy policy = SimulationSystemsTestData.CreatePolicy();
            CityEnvironmentalConditionState state = SimulationSystemsTestData.CreateState();

            Assert.ThrowsAny<Exception>(() => policy.Advance(
                state: state,
                pressure: CreateHeavyPressure(),
                fromUtc: SimulationSystemsTestData.CreatedAtUtc.AddHours(1),
                toUtc: SimulationSystemsTestData.CreatedAtUtc));
        }

        private static CitySystemPressureProfile CreateHeavyPressure()
        {
            return new CitySystemPressureProfile(
                rainPressure: 0.95m,
                snowPressure: 0.88m,
                stormPressure: 0.91m,
                freezePressure: 0.67m,
                thawRelief: 0.04m,
                drainageSupport: 0.08m,
                snowRemovalSupport: 0.10m,
                roadSupport: 0.12m,
                powerSupport: 0.07m,
                utilityIncidentSupport: 0.06m,
                heatingSupport: 0.09m,
                waterSupport: 0.08m,
                sanitationSupport: 0.07m);
        }
    }
}
