using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot;

public sealed class ApplyCityEssentialsSnapshotCommandHandlerTests
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

        ApplyCityEssentialsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEssentialsSnapshotStatus.Duplicate, result.Status);
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
        var stateRepository = new FakeCityPopulationEssentialsStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            stateRepository: stateRepository);

        ApplyCityEssentialsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEssentialsSnapshotStatus.CityDeleted, result.Status);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenStateIsStale_ReturnsStale()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationEssentialsStateRepository
        {
            State = CityPopulationEssentialsState.Create(
                cityId: CityId.From(cityId),
                supplyStressIndex: 1.10m,
                emergencyRationingEnabled: false,
                foodStockLevelIndex: 1.20m,
                foodShortageRiskIndex: 0.40m,
                medicineStockLevelIndex: 1.15m,
                medicineShortageRiskIndex: 0.35m,
                emergencyWaterStockLevelIndex: 1.25m,
                emergencyWaterShortageRiskIndex: 0.30m,
                effectiveTickId: 12,
                effectiveAtUtc: new DateTimeOffset(2048, 5, 3, 20, 40, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 3, 20, 41, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityEssentialsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEssentialsSnapshotStatus.Stale, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationEssentialsStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityEssentialsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityEssentialsSnapshotStatus.Applied, result.Status);
        CityPopulationEssentialsState state = Assert.Single(stateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(1.10m, state.SupplyStressIndex);
        Assert.True(state.EmergencyRationingEnabled);
        Assert.Equal(12, state.EffectiveTickId);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 20, 30, 0, TimeSpan.Zero), state.EffectiveAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityEssentialsSnapshotCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationEssentialsStateRepository? stateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityEssentialsSnapshotCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            stateRepository ?? new FakeCityPopulationEssentialsStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityEssentialsSnapshotCommand CreateCommand()
    {
        return new ApplyCityEssentialsSnapshotCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-essentials",
            SupplyStressIndex: 1.10m,
            EmergencyRationingEnabled: true,
            FoodStockLevelIndex: 1.20m,
            FoodShortageRiskIndex: 0.40m,
            MedicineStockLevelIndex: 1.15m,
            MedicineShortageRiskIndex: 0.35m,
            EmergencyWaterStockLevelIndex: 1.25m,
            EmergencyWaterShortageRiskIndex: 0.30m,
            EffectiveTickId: 12,
            EffectiveAtUtc: new DateTimeOffset(2048, 5, 3, 20, 30, 0, TimeSpan.Zero));
    }
}
