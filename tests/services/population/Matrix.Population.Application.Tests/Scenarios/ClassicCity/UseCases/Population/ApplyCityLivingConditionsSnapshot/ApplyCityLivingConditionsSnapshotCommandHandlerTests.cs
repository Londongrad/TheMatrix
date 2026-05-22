using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot;

public sealed class ApplyCityLivingConditionsSnapshotCommandHandlerTests
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

        ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityLivingConditionsSnapshotStatus.Duplicate, result.Status);
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
        var stateRepository = new FakeCityPopulationLivingConditionsStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            stateRepository: stateRepository);

        ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityLivingConditionsSnapshotStatus.CityDeleted, result.Status);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenStateIsStale_ReturnsStale()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationLivingConditionsStateRepository
        {
            State = CityPopulationLivingConditionsState.Create(
                cityId: CityId.From(cityId),
                floodingIndex: 0.20m,
                roadAccessibilityIndex: 0.90m,
                powerCoverageIndex: 1.05m,
                utilityContinuityIndex: 0.95m,
                heatingCoverageIndex: 0.88m,
                waterCoverageIndex: 1.02m,
                sanitationCoverageIndex: 0.97m,
                effectiveTickId: 8,
                effectiveAtUtc: new DateTimeOffset(2048, 5, 3, 21, 20, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 3, 21, 21, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityLivingConditionsSnapshotStatus.Stale, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationLivingConditionsStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityLivingConditionsSnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityLivingConditionsSnapshotStatus.Applied, result.Status);
        CityPopulationLivingConditionsState state = Assert.Single(stateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(0.20m, state.FloodingIndex);
        Assert.Equal(0.90m, state.RoadAccessibilityIndex);
        Assert.Equal(8, state.EffectiveTickId);
        Assert.Equal(new DateTimeOffset(2048, 5, 3, 21, 10, 0, TimeSpan.Zero), state.EffectiveAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityLivingConditionsSnapshotCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationLivingConditionsStateRepository? stateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityLivingConditionsSnapshotCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            stateRepository ?? new FakeCityPopulationLivingConditionsStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityLivingConditionsSnapshotCommand CreateCommand()
    {
        return new ApplyCityLivingConditionsSnapshotCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-living-conditions",
            FloodingIndex: 0.20m,
            RoadAccessibilityIndex: 0.90m,
            PowerCoverageIndex: 1.05m,
            UtilityContinuityIndex: 0.95m,
            HeatingCoverageIndex: 0.88m,
            WaterCoverageIndex: 1.02m,
            SanitationCoverageIndex: 0.97m,
            EffectiveTickId: 8,
            EffectiveAtUtc: new DateTimeOffset(2048, 5, 3, 21, 10, 0, TimeSpan.Zero));
    }
}
