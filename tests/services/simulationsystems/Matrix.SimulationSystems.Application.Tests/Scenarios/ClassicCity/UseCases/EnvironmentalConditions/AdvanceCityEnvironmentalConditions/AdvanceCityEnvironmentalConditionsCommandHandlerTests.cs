using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions
{
    public sealed class AdvanceCityEnvironmentalConditionsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            AdvanceCityEnvironmentalConditionsCommandHandler handler = CreateHandler(repository);

            AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(tickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEnvironmentalConditionsStatus.NotInitialized,
                actual: result.Status);
            Assert.Equal(
                expected: 0m,
                actual: result.ProcessedSimMinutes);
        }

        [Fact]
        public async Task Handle_WhenRequestIsOutOfOrder_ReturnsOutOfOrder()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.MarkTickApplied(6);
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            AdvanceCityEnvironmentalConditionsCommandHandler handler = CreateHandler(repository);

            AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(
                    fromUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    toUtc: SimulationSystemsApplicationTestSupport.LaterUtc,
                    tickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEnvironmentalConditionsStatus.OutOfOrder,
                actual: result.Status);
            Assert.Equal(
                expected: 0m,
                actual: result.ProcessedSimMinutes);
        }

        [Fact]
        public async Task Handle_WhenTickWasAlreadyApplied_ReturnsDuplicate()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            state.MarkTickApplied(5);
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            AdvanceCityEnvironmentalConditionsCommandHandler handler = CreateHandler(repository);

            AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(tickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEnvironmentalConditionsStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0m,
                actual: result.ProcessedSimMinutes);
        }

        [Fact]
        public async Task Handle_WhenAdvanceSucceeds_UpdatesStateAndWritesOutboxUsingTimeProvider()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            var systemsOutbox = new FakeCitySystemsResourceDemandOutboxWriter();
            var populationOutbox = new FakeCityPopulationLivingConditionsOutboxWriter();
            DateTimeOffset occurredAtUtc = SimulationSystemsApplicationTestSupport.CreatedAtUtc.AddHours(6);
            AdvanceCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                systemsOutbox: systemsOutbox,
                populationOutbox: populationOutbox,
                timeProvider: new FrozenTimeProvider(occurredAtUtc));

            AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(
                    fromUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                    toUtc: SimulationSystemsApplicationTestSupport.LaterUtc,
                    tickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEnvironmentalConditionsStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 120m,
                actual: result.ProcessedSimMinutes);
            Assert.Equal(
                expected: 5,
                actual: state.LastAppliedTickId);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.LaterUtc,
                actual: state.LastEvaluatedAtUtc);
            Assert.Single(systemsOutbox.Snapshots);
            Assert.Single(populationOutbox.Snapshots);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: systemsOutbox.Snapshots[0].OccurredAtUtc);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: populationOutbox.Snapshots[0].OccurredAtUtc);
            Assert.Equal(
                expected: 5,
                actual: systemsOutbox.Snapshots[0].EffectiveTickId);
            Assert.Equal(
                expected: 5,
                actual: populationOutbox.Snapshots[0].EffectiveTickId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handle_WhenSaveHitsConcurrency_ReturnsDuplicate()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork
            {
                SaveException = new DbUpdateConcurrencyException("race")
            };
            var systemsOutbox = new FakeCitySystemsResourceDemandOutboxWriter();
            var populationOutbox = new FakeCityPopulationLivingConditionsOutboxWriter();
            AdvanceCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                systemsOutbox: systemsOutbox,
                populationOutbox: populationOutbox);

            AdvanceCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(tickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEnvironmentalConditionsStatus.Duplicate,
                actual: result.Status);
            Assert.Single(systemsOutbox.Snapshots);
            Assert.Single(populationOutbox.Snapshots);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        private static AdvanceCityEnvironmentalConditionsCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork? unitOfWork = null,
            FakeCitySystemsResourceDemandOutboxWriter? systemsOutbox = null,
            FakeCityPopulationLivingConditionsOutboxWriter? populationOutbox = null,
            TimeProvider? timeProvider = null)
        {
            return new AdvanceCityEnvironmentalConditionsCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork ?? new FakeUnitOfWork(),
                systemsResourceDemandOutboxWriter: systemsOutbox ?? new FakeCitySystemsResourceDemandOutboxWriter(),
                populationLivingConditionsOutboxWriter: populationOutbox ??
                                                        new FakeCityPopulationLivingConditionsOutboxWriter(),
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                timeProvider: timeProvider ?? SimulationSystemsApplicationTestSupport.CreateTimeProvider());
        }

        private static AdvanceCityEnvironmentalConditionsCommand CreateCommand(
            DateTimeOffset? fromUtc = null,
            DateTimeOffset? toUtc = null,
            long tickId = 5)
        {
            return new AdvanceCityEnvironmentalConditionsCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                FromSimTimeUtc: fromUtc ?? SimulationSystemsApplicationTestSupport.CreatedAtUtc,
                ToSimTimeUtc: toUtc ?? SimulationSystemsApplicationTestSupport.LaterUtc,
                TickId: tickId);
        }
    }
}
