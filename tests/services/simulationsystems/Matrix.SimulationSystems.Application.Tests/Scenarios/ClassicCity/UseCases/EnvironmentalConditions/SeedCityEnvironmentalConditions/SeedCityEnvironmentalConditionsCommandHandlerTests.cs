using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions
{
    public sealed class SeedCityEnvironmentalConditionsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenSimulationKindDoesNotMatch_ReturnsIgnored()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var unitOfWork = new FakeUnitOfWork();
            var outbox = new FakeCityPopulationLivingConditionsOutboxWriter();
            SeedCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outbox: outbox);

            SeedCityEnvironmentalConditionsResult result = await handler.Handle(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    SimulationKind: "Sandbox",
                    DevelopmentLevel: "standard"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityEnvironmentalConditionsStatus.IgnoredSimulationKind,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Empty(outbox.Snapshots);
        }

        [Fact]
        public async Task Handle_WhenStateAlreadyExists_ReturnsDuplicate()
        {
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = SimulationSystemsApplicationTestSupport.CreateState()
            };
            SeedCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                outbox: new FakeCityPopulationLivingConditionsOutboxWriter());

            SeedCityEnvironmentalConditionsResult result = await handler.Handle(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    SimulationKind: "ClassicCity",
                    DevelopmentLevel: "standard"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityEnvironmentalConditionsStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: repository.State.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
        }

        [Fact]
        public async Task Handle_WhenDeletionTombstoneExists_ReturnsCityDeleted()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var unitOfWork = new FakeUnitOfWork();
            var outbox = new FakeCityPopulationLivingConditionsOutboxWriter();
            SeedCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outbox: outbox,
                deletionStateRepository: new FakeCitySystemsDeletionStateRepository
                {
                    DeletedAtUtc = SimulationSystemsApplicationTestSupport.LaterUtc
                });

            SeedCityEnvironmentalConditionsResult result = await handler.Handle(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    SimulationKind: "ClassicCity",
                    DevelopmentLevel: "advanced"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(SeedCityEnvironmentalConditionsStatus.CityDeleted, result.Status);
            Assert.Equal(0, repository.AddCallCount);
            Assert.Equal(0, unitOfWork.SaveChangesCallCount);
            Assert.Empty(outbox.Snapshots);
        }

        [Fact]
        public async Task Handle_WhenSeedSucceeds_AddsStateAndWritesOutboxWithInjectedTime()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var unitOfWork = new FakeUnitOfWork();
            var outbox = new FakeCityPopulationLivingConditionsOutboxWriter();
            DateTimeOffset occurredAtUtc = SimulationSystemsApplicationTestSupport.CreatedAtUtc.AddHours(4);
            SeedCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outbox: outbox,
                timeProvider: new FrozenTimeProvider(occurredAtUtc));

            SeedCityEnvironmentalConditionsResult result = await handler.Handle(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    CreatedAtUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    SimulationKind: "ClassicCity",
                    DevelopmentLevel: "advanced"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SeedCityEnvironmentalConditionsStatus.Applied,
                actual: result.Status);
            Assert.NotNull(repository.State);
            Assert.Equal(
                expected: 1,
                actual: repository.AddCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Single(outbox.Snapshots);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: outbox.Snapshots[0].OccurredAtUtc);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: outbox.Snapshots[0].CityId);
            Assert.Equal(
                expected: repository.State.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
        }

        private static SeedCityEnvironmentalConditionsCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork unitOfWork,
            FakeCityPopulationLivingConditionsOutboxWriter outbox,
            TimeProvider? timeProvider = null,
            FakeCitySystemsDeletionStateRepository? deletionStateRepository = null)
        {
            return new SeedCityEnvironmentalConditionsCommandHandler(
                repository: repository,
                deletionStateRepository: deletionStateRepository ?? new FakeCitySystemsDeletionStateRepository(),
                unitOfWork: unitOfWork,
                populationLivingConditionsOutboxWriter: outbox,
                policy: new CityEnvironmentalConditionPolicy(),
                timeProvider: timeProvider ?? SimulationSystemsApplicationTestSupport.CreateTimeProvider());
        }
    }
}
