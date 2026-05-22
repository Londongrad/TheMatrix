using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot;

public sealed class ApplyCityCostOfLivingSnapshotCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
    {
        var processedRepository = new FakeProcessedIntegrationMessageRepository
        {
            TryMarkProcessedResult = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            processedRepository: processedRepository,
            unitOfWork: unitOfWork);

        ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityCostOfLivingSnapshotStatus.Duplicate, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var stateRepository = new FakeCityPopulationCostOfLivingStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            stateRepository: stateRepository);

        ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityCostOfLivingSnapshotStatus.CityDeleted, result.Status);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenStateIsStale_ReturnsStale()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationCostOfLivingStateRepository
        {
            State = CityPopulationCostOfLivingState.Create(
                cityId: CityId.From(cityId),
                wageMultiplier: 1.10m,
                retailPriceMultiplier: 1.05m,
                housingCostMultiplier: 1.20m,
                utilityCostMultiplier: 1.15m,
                costOfLivingIndex: 1.18m,
                affordabilityIndex: 0.92m,
                lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 3, 20, 10, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 3, 20, 11, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityCostOfLivingSnapshotStatus.Stale, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationCostOfLivingStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityCostOfLivingSnapshotStatus.Applied, result.Status);
        CityPopulationCostOfLivingState state = Assert.Single(stateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(1.10m, state.WageMultiplier);
        Assert.Equal(1.18m, state.CostOfLivingIndex);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 20, 0, 0, TimeSpan.Zero), state.LastEvaluatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityCostOfLivingSnapshotCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationCostOfLivingStateRepository? stateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityCostOfLivingSnapshotCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            stateRepository ?? new FakeCityPopulationCostOfLivingStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityCostOfLivingSnapshotCommand CreateCommand()
    {
        return new ApplyCityCostOfLivingSnapshotCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-cost-of-living",
            WageMultiplier: 1.10m,
            RetailPriceMultiplier: 1.05m,
            HousingCostMultiplier: 1.20m,
            UtilityCostMultiplier: 1.15m,
            CostOfLivingIndex: 1.18m,
            AffordabilityIndex: 0.92m,
            OccurredAtUtc: new DateTimeOffset(2048, 5, 3, 20, 0, 0, TimeSpan.Zero));
    }
}
