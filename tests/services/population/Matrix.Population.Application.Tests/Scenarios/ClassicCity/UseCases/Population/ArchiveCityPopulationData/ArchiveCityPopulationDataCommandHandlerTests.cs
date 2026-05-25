using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData
{
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
            ArchiveCityPopulationDataCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ArchiveCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ArchiveCityPopulationDataStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCityIsDeleted_ReturnsCityDeleted()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow.AddDays(-1),
                    updatedAtUtc: UtcNow)
            };
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository();
            ArchiveCityPopulationDataCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                archiveStateRepository: archiveStateRepository);

            ArchiveCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ArchiveCityPopulationDataStatus.CityDeleted,
                actual: result.Status);
            Assert.Empty(archiveStateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenArchiveTimestampIsOlderThanExisting_ReturnsStale()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 13,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 13,
                        minute: 31,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            ArchiveCityPopulationDataCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                unitOfWork: unitOfWork);

            ArchiveCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ArchiveCityPopulationDataStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenArchiveStateDoesNotExist_CreatesArchiveState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            ArchiveCityPopulationDataCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                unitOfWork: unitOfWork);

            ArchiveCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ArchiveCityPopulationDataStatus.Applied,
                actual: result.Status);
            CityPopulationArchiveState state = Assert.Single(archiveStateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.ArchivedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenArchiveStateExists_UpdatesArchiveTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var existingState = CityPopulationArchiveState.Create(
                cityId: CityId.From(cityId),
                archivedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 1,
                    second: 0,
                    offset: TimeSpan.Zero));
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = existingState
            };
            var unitOfWork = new FakeUnitOfWork();
            ArchiveCityPopulationDataCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                unitOfWork: unitOfWork);

            ArchiveCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ArchiveCityPopulationDataStatus.Applied,
                actual: result.Status);
            Assert.Empty(archiveStateRepository.AddedStates);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: existingState.ArchivedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static ArchiveCityPopulationDataCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ArchiveCityPopulationDataCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ArchiveCityPopulationDataCommand CreateCommand()
        {
            return new ArchiveCityPopulationDataCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-archive",
                ArchivedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
