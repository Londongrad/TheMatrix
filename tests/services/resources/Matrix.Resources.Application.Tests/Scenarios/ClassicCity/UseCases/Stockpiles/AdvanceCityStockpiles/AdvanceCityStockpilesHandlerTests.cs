using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles
{
    public sealed class AdvanceCityStockpilesHandlerTests
    {
        [Fact]
        public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
        {
            var handler = new AdvanceCityStockpilesCommandHandler(
                repository: new FakeCityStockpileRepository(),
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityStockpileSnapshotOutboxWriter(),
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            AdvanceCityStockpilesResult result = await handler.Handle(
                request: new AdvanceCityStockpilesCommand(
                    CityId: CityId,
                    FromSimTimeUtc: CreatedAtUtc,
                    ToSimTimeUtc: LaterUtc,
                    TickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityStockpilesStatus.NotInitialized,
                actual: result.Status);
        }

        [Fact]
        public async Task Handler_ReturnsOutOfOrderWhenTickMovesBackward()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.MarkTickApplied(5);
            var handler = new AdvanceCityStockpilesCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityStockpileSnapshotOutboxWriter(),
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            AdvanceCityStockpilesResult result = await handler.Handle(
                request: new AdvanceCityStockpilesCommand(
                    CityId: CityId,
                    FromSimTimeUtc: CreatedAtUtc,
                    ToSimTimeUtc: LaterUtc,
                    TickId: 4),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityStockpilesStatus.OutOfOrder,
                actual: result.Status);
        }

        [Fact]
        public async Task Handler_ReturnsDuplicateWhenTickWasAlreadyApplied()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.MarkTickApplied(5);
            var handler = new AdvanceCityStockpilesCommandHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork(),
                outboxWriter: new FakeCityStockpileSnapshotOutboxWriter(),
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider());

            AdvanceCityStockpilesResult result = await handler.Handle(
                request: new AdvanceCityStockpilesCommand(
                    CityId: CityId,
                    FromSimTimeUtc: CreatedAtUtc,
                    ToSimTimeUtc: LaterUtc,
                    TickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityStockpilesStatus.Duplicate,
                actual: result.Status);
        }

        [Fact]
        public async Task Handler_AdvancesStateAppliesDueResupplyAndWritesSnapshot()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.MarkTickApplied(4);
            repository.State.ScheduleResupply(
                focus: ResupplyFocus.Fuel,
                intensity: ResupplyIntensity.Medium,
                focusDistrictId: null,
                readyAtTickId: 5);
            var unitOfWork = new FakeUnitOfWork();
            var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
            DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(20);
            var handler = new AdvanceCityStockpilesCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter,
                policy: new CityStockpilePolicy(),
                timeProvider: CreateTimeProvider(occurredAtUtc));

            AdvanceCityStockpilesResult result = await handler.Handle(
                request: new AdvanceCityStockpilesCommand(
                    CityId: CityId,
                    FromSimTimeUtc: CreatedAtUtc,
                    ToSimTimeUtc: LaterUtc,
                    TickId: 5),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityStockpilesStatus.Applied,
                actual: result.Status);
            Assert.True(result.ProcessedSimMinutes > 0);
            Assert.Equal(
                expected: 5,
                actual: repository.State!.LastAppliedTickId);
            Assert.False(repository.State.PendingResupply.IsScheduled);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Single(outboxWriter.Snapshots);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: outboxWriter.Snapshots[0].OccurredAtUtc);
        }
    }
}
