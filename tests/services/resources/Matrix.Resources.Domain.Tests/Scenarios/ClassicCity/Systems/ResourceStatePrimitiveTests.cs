using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Systems
{
    public sealed class ResourceStatePrimitiveTests
    {
        [Fact]
        public void CityResourceStockLineState_Create_RoundsAndMapsSnapshot()
        {
            CityStockpileLineSnapshot snapshot = CreateLine(
                kind: CityResourceKind.Filters,
                stockLevelIndex: 0.12345m,
                demandPressureIndex: 0.33335m,
                resupplyReadinessIndex: 0.44445m,
                shortageRiskIndex: 0.55555m);

            var state = CityResourceStockLineState.Create(snapshot);

            Assert.Equal(
                expected: 0.1235m,
                actual: state.StockLevelIndex);
            Assert.Equal(
                expected: 0.3334m,
                actual: state.DemandPressureIndex);
            Assert.Equal(
                expected: 0.4445m,
                actual: state.ResupplyReadinessIndex);
            Assert.Equal(
                expected: 0.5556m,
                actual: state.ShortageRiskIndex);
            Assert.Equal(
                expected: snapshot.Kind,
                actual: state.Kind);
        }

        [Fact]
        public void CityResourceStockLineState_Create_WithInvalidEnum_ThrowsDomainException()
        {
            CityStockpileLineSnapshot snapshot = CreateLine((CityResourceKind)99);

            Assert.Throws<DomainException>(() => CityResourceStockLineState.Create(snapshot));
        }

        [Fact]
        public void CityResourceStockLineState_ApplySnapshot_WithOutOfRangeIndex_ThrowsDomainException()
        {
            var state = CityResourceStockLineState.Create(CreateLine(CityResourceKind.Food));

            Assert.Throws<DomainException>(() => state.ApplySnapshot(
                CreateLine(
                    kind: CityResourceKind.Food,
                    stockLevelIndex: 1.10m)));
        }

        [Fact]
        public void CityPendingResupplyState_ScheduleReadinessAndClear_WorkAsExpected()
        {
            var state = CityPendingResupplyState.None();
            var focusDistrictId = Guid.Parse("40000000-0000-0000-0000-000000000001");

            state.Schedule(
                focus: ResupplyFocus.Medicine,
                intensity: ResupplyIntensity.High,
                focusDistrictId: focusDistrictId,
                readyAtTickId: 7);

            Assert.True(state.IsScheduled);
            Assert.Equal(
                expected: nameof(ResupplyFocus.Medicine),
                actual: state.Focus);
            Assert.Equal(
                expected: nameof(ResupplyIntensity.High),
                actual: state.Intensity);
            Assert.Equal(
                expected: focusDistrictId,
                actual: state.FocusDistrictId);
            Assert.False(state.IsReady(6));
            Assert.True(state.IsReady(7));

            state.Clear();

            Assert.False(state.IsScheduled);
            Assert.Equal(
                expected: string.Empty,
                actual: state.Focus);
            Assert.Equal(
                expected: 0,
                actual: state.ReadyAtTickId);
        }

        [Fact]
        public void CitySystemsAndBudgetStates_CreateApplyAndRoundtrip_WorkAsExpected()
        {
            CitySystemsResourceDemandSnapshot initialDemand = CreateSystemsDemand(effectiveTickId: 3);
            CityOperationalBudgetPressureSnapshot initialBudget = CreateBudgetPressure(effectiveTickId: 3);
            var demandState = CitySystemsResourceDemandState.Create(initialDemand);
            var budgetState = CityOperationalBudgetPressureState.Create(initialBudget);
            CitySystemsResourceDemandSnapshot nextDemand = CreateSystemsDemand(
                fuelDemandPressureIndex: 0.61m,
                effectiveTickId: 8,
                effectiveAtUtc: CreatedAtUtc.AddHours(2));
            CityOperationalBudgetPressureSnapshot nextBudget = CreateBudgetPressure(
                balance: 120_000m,
                pressureIndex: 0.72m,
                effectiveTickId: 8,
                effectiveAtUtc: CreatedAtUtc.AddHours(2));

            demandState.ApplySnapshot(nextDemand);
            budgetState.ApplySnapshot(nextBudget);

            Assert.Equal(
                expected: nextDemand,
                actual: demandState.ToSnapshot());
            Assert.Equal(
                expected: nextBudget,
                actual: budgetState.ToSnapshot());
        }

        [Fact]
        public void NeutralSnapshots_NormalizeTickAndUtc()
        {
            DateTimeOffset nonUtc = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 18,
                minute: 0,
                second: 0,
                offset: TimeSpan.FromHours(9));

            var demand = CitySystemsResourceDemandSnapshot.Neutral(
                effectiveAtUtc: nonUtc,
                effectiveTickId: -5);
            var budget = CityOperationalBudgetPressureSnapshot.Neutral(
                effectiveAtUtc: nonUtc,
                effectiveTickId: -8);

            Assert.Equal(
                expected: 0,
                actual: demand.EffectiveTickId);
            Assert.Equal(
                expected: 0,
                actual: budget.EffectiveTickId);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: demand.EffectiveAtUtc.Offset);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: budget.EffectiveAtUtc.Offset);
        }
    }
}
