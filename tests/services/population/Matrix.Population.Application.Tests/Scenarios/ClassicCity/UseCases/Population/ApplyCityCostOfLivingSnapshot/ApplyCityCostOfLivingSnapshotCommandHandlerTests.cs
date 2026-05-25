using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot
{
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
            ApplyCityCostOfLivingSnapshotCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityCostOfLivingSnapshotStatus.Duplicate,
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
            var stateRepository = new FakeCityPopulationCostOfLivingStateRepository();
            ApplyCityCostOfLivingSnapshotCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                stateRepository: stateRepository);

            ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityCostOfLivingSnapshotStatus.CityDeleted,
                actual: result.Status);
            Assert.Empty(stateRepository.AddedStates);
        }

        [Fact]
        public async Task Handle_WhenStateIsStale_ReturnsStale()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
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
                    lastEvaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 10,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 20,
                        minute: 11,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityCostOfLivingSnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityCostOfLivingSnapshotStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_CreatesSnapshotState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var stateRepository = new FakeCityPopulationCostOfLivingStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            ApplyCityCostOfLivingSnapshotCommandHandler handler = CreateHandler(
                stateRepository: stateRepository,
                unitOfWork: unitOfWork);

            ApplyCityCostOfLivingSnapshotResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: ApplyCityCostOfLivingSnapshotStatus.Applied,
                actual: result.Status);
            CityPopulationCostOfLivingState state = Assert.Single(stateRepository.AddedStates);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: state.CityId);
            Assert.Equal(
                expected: 1.10m,
                actual: state.WageMultiplier);
            Assert.Equal(
                expected: 1.18m,
                actual: state.CostOfLivingIndex);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 20,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: state.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static ApplyCityCostOfLivingSnapshotCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationCostOfLivingStateRepository? stateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new ApplyCityCostOfLivingSnapshotCommandHandler(
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationCostOfLivingStateRepository: stateRepository ??
                                                           new FakeCityPopulationCostOfLivingStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
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
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 20,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
