using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;

public sealed class SyncCitySystemsDemandHandlerTests
{
    [Fact]
    public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
    {
        var handler = new SyncCitySystemsDemandCommandHandler(
            new FakeCityStockpileRepository(),
            new FakeUnitOfWork(),
            new CityStockpilePolicy());

        SyncCitySystemsDemandResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 8, effectiveAtUtc: LaterUtc),
            CancellationToken.None);

        Assert.Equal(SyncCitySystemsDemandStatus.NotInitialized, result.Status);
        Assert.Equal(0m, result.OverallDemandPressureIndex);
    }

    [Fact]
    public async Task Handler_ReturnsStaleWhenSnapshotMovesBackward()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.ApplySystemsDemand(new CitySystemsResourceDemandSnapshot(
            FuelDemandPressureIndex: 0.42m,
            SparePartsDemandPressureIndex: 0.31m,
            FiltersDemandPressureIndex: 0.26m,
            EmergencyWaterDemandPressureIndex: 0.19m,
            OverallDemandPressureIndex: 0.33m,
            EffectiveTickId: 7,
            EffectiveAtUtc: LaterUtc));
        var handler = new SyncCitySystemsDemandCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new CityStockpilePolicy());

        SyncCitySystemsDemandResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 7, effectiveAtUtc: CreatedAtUtc.AddMinutes(30), overallDemandPressureIndex: 0.55m),
            CancellationToken.None);

        Assert.Equal(SyncCitySystemsDemandStatus.Stale, result.Status);
        Assert.Equal(0.33m, result.OverallDemandPressureIndex);
    }

    [Fact]
    public async Task Handler_DefersWhenSnapshotIsAheadOfCurrentProgress()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        decimal originalFuelStock = repository.State.Fuel.StockLevelIndex;
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SyncCitySystemsDemandCommandHandler(
            repository,
            unitOfWork,
            new CityStockpilePolicy());

        SyncCitySystemsDemandResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 6, effectiveAtUtc: LaterUtc, overallDemandPressureIndex: 0.61m),
            CancellationToken.None);

        Assert.Equal(SyncCitySystemsDemandStatus.Deferred, result.Status);
        Assert.Equal(0.61m, repository.State!.SystemsDemand.OverallDemandPressureIndex);
        Assert.Equal(originalFuelStock, repository.State.Fuel.StockLevelIndex);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handler_AppliesSnapshotWhenItMatchesCurrentProgress()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.MarkTickApplied(6);
        decimal originalFuelDemand = repository.State.Fuel.DemandPressureIndex;
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SyncCitySystemsDemandCommandHandler(
            repository,
            unitOfWork,
            new CityStockpilePolicy());

        SyncCitySystemsDemandResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 6, effectiveAtUtc: CreatedAtUtc, overallDemandPressureIndex: 0.58m),
            CancellationToken.None);

        Assert.Equal(SyncCitySystemsDemandStatus.Applied, result.Status);
        Assert.Equal(0.58m, repository.State!.SystemsDemand.OverallDemandPressureIndex);
        Assert.NotEqual(originalFuelDemand, repository.State.Fuel.DemandPressureIndex);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static SyncCitySystemsDemandCommand CreateCommand(
        long effectiveTickId,
        DateTimeOffset effectiveAtUtc,
        decimal overallDemandPressureIndex = 0.47m)
    {
        return new SyncCitySystemsDemandCommand(
            CityId: CityId,
            FuelDemandPressureIndex: 0.55m,
            SparePartsDemandPressureIndex: 0.41m,
            FiltersDemandPressureIndex: 0.38m,
            EmergencyWaterDemandPressureIndex: 0.29m,
            OverallDemandPressureIndex: overallDemandPressureIndex,
            EffectiveTickId: effectiveTickId,
            EffectiveAtUtc: effectiveAtUtc);
    }
}
