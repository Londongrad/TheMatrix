using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;

public sealed class SyncCityResourceSupplyCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        SyncCityResourceSupplyResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(SyncCityResourceSupplyStatus.NotInitialized, result.Status);
    }

    [Fact]
    public async Task Handle_WhenSnapshotIsStale_ReturnsStale()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyResourceSupply(new Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models.CityResourceSupplySnapshot(
            0.20m, 0.60m, 0.60m, 0.20m, 0.60m, 0.60m, 0.20m, 0.60m, 0.60m, 0.20m, 0.60m, 0.60m, 0.20m, 7, SimulationSystemsApplicationTestSupport.LaterUtc));
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        SyncCityResourceSupplyResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 6, effectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityResourceSupplyStatus.Stale, result.Status);
        Assert.Equal(7, result.EffectiveTickId);
    }

    [Fact]
    public async Task Handle_WhenSnapshotIsAheadOfCurrentProgress_ReturnsDeferred()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.MarkTickApplied(3);
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        SyncCityResourceSupplyResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 5, effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityResourceSupplyStatus.Deferred, result.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(5, state.ResourceSupply.EffectiveTickId);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreatedAtUtc, state.LastEvaluatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenSnapshotMatchesCurrentProgress_ReturnsApplied()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.MarkTickApplied(5);
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        SyncCityResourceSupplyResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 5, effectiveAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityResourceSupplyStatus.Applied, result.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(0.32m, state.ResourceSupply.SupplyStressIndex);
        Assert.Equal(state.LastEvaluatedAtUtc, SimulationSystemsApplicationTestSupport.CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenConcurrencyPersistsMatchingSnapshot_ReturnsConcurrent()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork
        {
            SaveException = new DbUpdateConcurrencyException("race")
        };
        var handler = CreateHandler(repository, unitOfWork);

        SyncCityResourceSupplyResult result = await handler.Handle(
            CreateCommand(effectiveTickId: 4, effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
            CancellationToken.None);

        Assert.Equal(SyncCityResourceSupplyStatus.Concurrent, result.Status);
        Assert.Equal(4, result.EffectiveTickId);
        Assert.Equal(SimulationSystemsApplicationTestSupport.LaterUtc, result.EffectiveAtUtc);
    }

    private static SyncCityResourceSupplyCommandHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeUnitOfWork unitOfWork)
    {
        return new SyncCityResourceSupplyCommandHandler(
            repository,
            unitOfWork,
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());
    }

    private static SyncCityResourceSupplyCommand CreateCommand(
        long effectiveTickId = 5,
        DateTimeOffset? effectiveAtUtc = null)
    {
        return new SyncCityResourceSupplyCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            SupplyStressIndex: 0.32m,
            FuelStockLevelIndex: 0.51m,
            FuelResupplyReadinessIndex: 0.61m,
            FuelShortageRiskIndex: 0.23m,
            SparePartsStockLevelIndex: 0.49m,
            SparePartsResupplyReadinessIndex: 0.58m,
            SparePartsShortageRiskIndex: 0.31m,
            FiltersStockLevelIndex: 0.44m,
            FiltersResupplyReadinessIndex: 0.57m,
            FiltersShortageRiskIndex: 0.28m,
            EmergencyWaterStockLevelIndex: 0.70m,
            EmergencyWaterResupplyReadinessIndex: 0.62m,
            EmergencyWaterShortageRiskIndex: 0.15m,
            EffectiveTickId: effectiveTickId,
            EffectiveAtUtc: effectiveAtUtc ?? SimulationSystemsApplicationTestSupport.CreatedAtUtc);
    }
}
