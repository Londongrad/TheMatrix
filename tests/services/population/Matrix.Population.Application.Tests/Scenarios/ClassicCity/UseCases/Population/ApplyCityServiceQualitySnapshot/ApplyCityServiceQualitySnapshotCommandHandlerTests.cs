using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot
{
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
            ApplyCityServiceQualitySnapshotCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ApplyCityServiceQualitySnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityServiceQualitySnapshotStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCityIsDeleted_ReturnsDeletedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var stateRepository = new FakeCityPopulationServiceQualityStateRepository();
            ApplyCityServiceQualitySnapshotCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                stateRepository: stateRepository);

            ApplyCityServiceQualitySnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityServiceQualitySnapshotStatus.CityDeleted,
                actual: result.Status);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenCityIsArchived_ReturnsArchivedStatus()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var stateRepository = new FakeCityPopulationServiceQualityStateRepository();
            ApplyCityServiceQualitySnapshotCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                stateRepository: stateRepository);

            ApplyCityServiceQualitySnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityServiceQualitySnapshotStatus.CityArchived,
                actual: result.Status);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenStateIsStale_ReturnsStale()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var stateRepository = new FakeCityPopulationServiceQualityStateRepository
            {
                State = CityPopulationServiceQualityState.Create(
                    cityId: CityId.From(cityId),
                    healthcareQualityIndex: 1.10m,
                    housingSupportIndex: 1.30m,
                    lastEvaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 12,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 12,
                        minute: 31,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityServiceQualitySnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityServiceQualitySnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityServiceQualitySnapshotStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var processedRepository = new FakeProcessedIntegrationMessageRepository();
            var stateRepository = new FakeCityPopulationServiceQualityStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityServiceQualitySnapshotCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityServiceQualitySnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityServiceQualitySnapshotStatus.Applied,
                actual: result.Status);
            CityPopulationServiceQualityState state = Assert.Single(stateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: 1.18m,
                actual: state.HealthcareQualityIndex);
            Assert.Equal(
                expected: 1.07m,
                actual: state.HousingSupportIndex);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                actual: processedRepository.RequestedMessageId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static ApplyCityServiceQualitySnapshotCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationServiceQualityStateRepository? stateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyCityServiceQualitySnapshotCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                cityPopulationServiceQualityStateRepository: stateRepository ??
                                                             new FakeCityPopulationServiceQualityStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static ApplyCityServiceQualitySnapshotCommand CreateCommand()
        {
            return new ApplyCityServiceQualitySnapshotCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-service-quality",
                HealthcareQualityIndex: 1.18m,
                HousingSupportIndex: 1.07m,
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
