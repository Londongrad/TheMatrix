using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Systems
{
    public sealed class CityStockpileStateTests
    {
        [Fact]
        public void Create_SeedsStateWithSnapshotAndZeroTick()
        {
            var state = CityStockpileState.Create(
                simulationHostId: CreateHostId(),
                seed: CreateSnapshot());

            Assert.Equal(
                expected: CreateHostId(),
                actual: state.SimulationHostId);
            Assert.Equal(
                expected: 0,
                actual: state.LastAppliedTickId);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: state.LastEvaluatedAtUtc);
            Assert.False(state.PendingResupply.IsScheduled);
        }

        [Fact]
        public void ApplySnapshot_WithOlderTimestamp_ThrowsInvalidOperationException()
        {
            var state = CityStockpileState.Create(
                simulationHostId: CreateHostId(),
                seed: CreateSnapshot(evaluatedAtUtc: CreatedAtUtc.AddHours(1)));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => state.ApplySnapshot(CreateSnapshot(evaluatedAtUtc: CreatedAtUtc)));

            Assert.Contains(
                expectedSubstring: "cannot move backwards",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ScheduleResupply_AndApplyDueResupply_RefreshesStateAndClearsPending()
        {
            var state = CityStockpileState.Create(
                simulationHostId: CreateHostId(),
                seed: CreateSnapshot());
            var policy = new CityStockpilePolicy();
            decimal previousMedicineStock = state.Medicine.StockLevelIndex;

            state.ScheduleResupply(
                focus: ResupplyFocus.Medicine,
                intensity: ResupplyIntensity.High,
                focusDistrictId: null,
                readyAtTickId: 4);
            bool appliedTooEarly = state.ApplyDueResupply(
                policy: policy,
                tickId: 3);
            bool applied = state.ApplyDueResupply(
                policy: policy,
                tickId: 4);

            Assert.False(appliedTooEarly);
            Assert.True(applied);
            Assert.False(state.PendingResupply.IsScheduled);
            Assert.True(state.Medicine.StockLevelIndex > previousMedicineStock);
        }

        [Fact]
        public void MarkTickApplied_RejectsBackwardsProgression()
        {
            var state = CityStockpileState.Create(
                simulationHostId: CreateHostId(),
                seed: CreateSnapshot());
            state.MarkTickApplied(5);

            Assert.Throws<InvalidOperationException>(() => state.MarkTickApplied(4));
        }
    }
}
