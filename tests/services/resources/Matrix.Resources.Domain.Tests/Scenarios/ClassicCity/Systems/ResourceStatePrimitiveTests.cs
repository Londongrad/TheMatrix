using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Systems;

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

        CityResourceStockLineState state = CityResourceStockLineState.Create(snapshot);

        Assert.Equal(0.1235m, state.StockLevelIndex);
        Assert.Equal(0.3334m, state.DemandPressureIndex);
        Assert.Equal(0.4445m, state.ResupplyReadinessIndex);
        Assert.Equal(0.5556m, state.ShortageRiskIndex);
        Assert.Equal(snapshot.Kind, state.Kind);
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
        CityResourceStockLineState state = CityResourceStockLineState.Create(CreateLine(CityResourceKind.Food));

        Assert.Throws<DomainException>(() => state.ApplySnapshot(CreateLine(CityResourceKind.Food, stockLevelIndex: 1.10m)));
    }

    [Fact]
    public void CityPendingResupplyState_ScheduleReadinessAndClear_WorkAsExpected()
    {
        CityPendingResupplyState state = CityPendingResupplyState.None();
        Guid focusDistrictId = Guid.Parse("40000000-0000-0000-0000-000000000001");

        state.Schedule(ResupplyFocus.Medicine, ResupplyIntensity.High, focusDistrictId, readyAtTickId: 7);

        Assert.True(state.IsScheduled);
        Assert.Equal(nameof(ResupplyFocus.Medicine), state.Focus);
        Assert.Equal(nameof(ResupplyIntensity.High), state.Intensity);
        Assert.Equal(focusDistrictId, state.FocusDistrictId);
        Assert.False(state.IsReady(6));
        Assert.True(state.IsReady(7));

        state.Clear();

        Assert.False(state.IsScheduled);
        Assert.Equal(string.Empty, state.Focus);
        Assert.Equal(0, state.ReadyAtTickId);
    }

    [Fact]
    public void CitySystemsAndBudgetStates_CreateApplyAndRoundtrip_WorkAsExpected()
    {
        CitySystemsResourceDemandSnapshot initialDemand = CreateSystemsDemand(effectiveTickId: 3);
        CityOperationalBudgetPressureSnapshot initialBudget = CreateBudgetPressure(effectiveTickId: 3);
        CitySystemsResourceDemandState demandState = CitySystemsResourceDemandState.Create(initialDemand);
        CityOperationalBudgetPressureState budgetState = CityOperationalBudgetPressureState.Create(initialBudget);
        CitySystemsResourceDemandSnapshot nextDemand = CreateSystemsDemand(fuelDemandPressureIndex: 0.61m, effectiveTickId: 8, effectiveAtUtc: CreatedAtUtc.AddHours(2));
        CityOperationalBudgetPressureSnapshot nextBudget = CreateBudgetPressure(balance: 120_000m, pressureIndex: 0.72m, effectiveTickId: 8, effectiveAtUtc: CreatedAtUtc.AddHours(2));

        demandState.ApplySnapshot(nextDemand);
        budgetState.ApplySnapshot(nextBudget);

        Assert.Equal(nextDemand, demandState.ToSnapshot());
        Assert.Equal(nextBudget, budgetState.ToSnapshot());
    }

    [Fact]
    public void NeutralSnapshots_NormalizeTickAndUtc()
    {
        DateTimeOffset nonUtc = new(2048, 6, 1, 18, 0, 0, TimeSpan.FromHours(9));

        CitySystemsResourceDemandSnapshot demand = CitySystemsResourceDemandSnapshot.Neutral(nonUtc, effectiveTickId: -5);
        CityOperationalBudgetPressureSnapshot budget = CityOperationalBudgetPressureSnapshot.Neutral(nonUtc, effectiveTickId: -8);

        Assert.Equal(0, demand.EffectiveTickId);
        Assert.Equal(0, budget.EffectiveTickId);
        Assert.Equal(TimeSpan.Zero, demand.EffectiveAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, budget.EffectiveAtUtc.Offset);
    }
}
