using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot;

public sealed class ApplyCityServiceQualitySnapshotCommandHandlerTests
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

        ApplyCityServiceQualitySnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityServiceQualitySnapshotStatus.Duplicate, result.Status);
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
        var stateRepository = new FakeCityPopulationServiceQualityStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            stateRepository: stateRepository);

        ApplyCityServiceQualitySnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityServiceQualitySnapshotStatus.CityDeleted, result.Status);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenCityIsArchived_ReturnsArchivedStatus()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
        {
            State = CityPopulationArchiveState.Create(
                cityId: CityId.From(cityId),
                archivedAtUtc: UtcNow.AddDays(-2),
                updatedAtUtc: UtcNow.AddDays(-1))
        };
        var stateRepository = new FakeCityPopulationServiceQualityStateRepository();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            stateRepository: stateRepository);

        ApplyCityServiceQualitySnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityServiceQualitySnapshotStatus.CityArchived, result.Status);
        Assert.Empty(stateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenStateIsStale_ReturnsStale()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var stateRepository = new FakeCityPopulationServiceQualityStateRepository
        {
            State = CityPopulationServiceQualityState.Create(
                cityId: CityId.From(cityId),
                healthcareQualityIndex: 1.10m,
                educationQualityIndex: 1.20m,
                housingSupportIndex: 1.30m,
                lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 30, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 31, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityServiceQualitySnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityServiceQualitySnapshotStatus.Stale, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var processedRepository = new FakeProcessedIntegrationMessageRepository();
        var stateRepository = new FakeCityPopulationServiceQualityStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            processedRepository: processedRepository,
            stateRepository: stateRepository,
            unitOfWork: unitOfWork);

        ApplyCityServiceQualitySnapshotResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ApplyCityServiceQualitySnapshotStatus.Applied, result.Status);
        CityPopulationServiceQualityState state = Assert.Single(stateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(1.18m, state.HealthcareQualityIndex);
        Assert.Equal(0.95m, state.EducationQualityIndex);
        Assert.Equal(1.07m, state.HousingSupportIndex);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero), state.LastEvaluatedAtUtc);
        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), processedRepository.RequestedMessageId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ApplyCityServiceQualitySnapshotCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeCityPopulationServiceQualityStateRepository? stateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ApplyCityServiceQualitySnapshotCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            stateRepository ?? new FakeCityPopulationServiceQualityStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ApplyCityServiceQualitySnapshotCommand CreateCommand()
    {
        return new ApplyCityServiceQualitySnapshotCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-service-quality",
            HealthcareQualityIndex: 1.18m,
            EducationQualityIndex: 0.95m,
            HousingSupportIndex: 1.07m,
            OccurredAtUtc: new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero));
    }
}
