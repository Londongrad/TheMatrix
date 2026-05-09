using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Systems;

public sealed class CityStockpileStateTests
{
    [Fact]
    public void Create_SeedsStateWithSnapshotAndZeroTick()
    {
        CityStockpileState state = CityStockpileState.Create(CreateHostId(), CreateSnapshot());

        Assert.Equal(CreateHostId(), state.SimulationHostId);
        Assert.Equal(0, state.LastAppliedTickId);
        Assert.Equal(CreatedAtUtc, state.LastEvaluatedAtUtc);
        Assert.False(state.PendingResupply.IsScheduled);
    }

    [Fact]
    public void ApplySnapshot_WithOlderTimestamp_ThrowsInvalidOperationException()
    {
        CityStockpileState state = CityStockpileState.Create(CreateHostId(), CreateSnapshot(evaluatedAtUtc: CreatedAtUtc.AddHours(1)));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => state.ApplySnapshot(CreateSnapshot(evaluatedAtUtc: CreatedAtUtc)));

        Assert.Contains("cannot move backwards", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScheduleResupply_AndApplyDueResupply_RefreshesStateAndClearsPending()
    {
        CityStockpileState state = CityStockpileState.Create(CreateHostId(), CreateSnapshot());
        var policy = new CityStockpilePolicy();
        decimal previousMedicineStock = state.Medicine.StockLevelIndex;

        state.ScheduleResupply(ResupplyFocus.Medicine, ResupplyIntensity.High, focusDistrictId: null, readyAtTickId: 4);
        bool appliedTooEarly = state.ApplyDueResupply(policy, tickId: 3);
        bool applied = state.ApplyDueResupply(policy, tickId: 4);

        Assert.False(appliedTooEarly);
        Assert.True(applied);
        Assert.False(state.PendingResupply.IsScheduled);
        Assert.True(state.Medicine.StockLevelIndex > previousMedicineStock);
    }

    [Fact]
    public void MarkTickApplied_RejectsBackwardsProgression()
    {
        CityStockpileState state = CityStockpileState.Create(CreateHostId(), CreateSnapshot());
        state.MarkTickApplied(5);

        Assert.Throws<InvalidOperationException>(() => state.MarkTickApplied(4));
    }
}
