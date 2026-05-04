using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;

public sealed class ArchiveCityPopulationDataCommandHandlerTests
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

        ArchiveCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ArchiveCityPopulationDataStatus.Duplicate, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCityIsDeleted_ReturnsCityDeleted()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: UtcNow.AddDays(-1),
                updatedAtUtc: UtcNow)
        };
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            archiveStateRepository: archiveStateRepository);

        ArchiveCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ArchiveCityPopulationDataStatus.CityDeleted, result.Status);
        Assert.Empty(archiveStateRepository.AddedStates);
    }

    [Fact]
    public async Task Handle_WhenArchiveTimestampIsOlderThanExisting_ReturnsStale()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
        {
            State = CityPopulationArchiveState.Create(
                cityId: CityId.From(cityId),
                archivedAtUtc: new DateTimeOffset(2048, 5, 4, 13, 30, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 4, 13, 31, 0, TimeSpan.Zero))
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            unitOfWork: unitOfWork);

        ArchiveCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ArchiveCityPopulationDataStatus.Stale, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenArchiveStateDoesNotExist_CreatesArchiveState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            unitOfWork: unitOfWork);

        ArchiveCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ArchiveCityPopulationDataStatus.Applied, result.Status);
        CityPopulationArchiveState state = Assert.Single(archiveStateRepository.AddedStates);
        Assert.Equal(CityId.From(cityId), state.CityId);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 13, 0, 0, TimeSpan.Zero), state.ArchivedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenArchiveStateExists_UpdatesArchiveTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityPopulationArchiveState existingState = CityPopulationArchiveState.Create(
            cityId: CityId.From(cityId),
            archivedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 1, 0, TimeSpan.Zero));
        var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
        {
            State = existingState
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            archiveStateRepository: archiveStateRepository,
            unitOfWork: unitOfWork);

        ArchiveCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(ArchiveCityPopulationDataStatus.Applied, result.Status);
        Assert.Empty(archiveStateRepository.AddedStates);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 13, 0, 0, TimeSpan.Zero), existingState.ArchivedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static ArchiveCityPopulationDataCommandHandler CreateHandler(
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ArchiveCityPopulationDataCommandHandler(
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static ArchiveCityPopulationDataCommand CreateCommand()
    {
        return new ArchiveCityPopulationDataCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-archive",
            ArchivedAtUtc: new DateTimeOffset(2048, 5, 4, 13, 0, 0, TimeSpan.Zero));
    }
}
