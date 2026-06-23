using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Lifecycle.DeleteEducationSimulation;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Application.Tests.Lifecycle.DeleteEducationSimulation
{
    public sealed class DeleteEducationSimulationCommandHandlerTests
    {
        private static readonly Guid HostId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTimeOffset DeletedAtUtc =
            new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset UpdatedAtUtc =
            new(2048, 5, 6, 10, 0, 1, TimeSpan.Zero);

        [Fact]
        public async Task Handle_FirstDeletion_DeletesDataAndRecordsTombstoneAtomically()
        {
            var repository = new DeletionRepositoryStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            DeleteEducationSimulationCommandHandler handler = CreateHandler(repository, unitOfWork);

            DeleteEducationSimulationResult result = await handler.Handle(
                request: CreateCommand(DeletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteEducationSimulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: new SimulationHostId(HostId),
                actual: repository.DeletedHostId);
            Assert.Equal(
                expected: DeletedAtUtc,
                actual: repository.RecordedDeletedAtUtc);
            Assert.Equal(
                expected: UpdatedAtUtc,
                actual: repository.RecordedUpdatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveCount);
            Assert.Equal(
                expected: System.Data.IsolationLevel.Serializable,
                actual: unitOfWork.LastIsolationLevel);
        }

        [Theory]
        [InlineData(0, DeleteEducationSimulationStatus.Duplicate)]
        [InlineData(1, DeleteEducationSimulationStatus.Stale)]
        public async Task Handle_AlreadyObservedDeletion_SkipsMutation(
            int recordedDeletionOffsetMinutes,
            DeleteEducationSimulationStatus expectedStatus)
        {
            var repository = new DeletionRepositoryStub
            {
                RecordedDeletion = DeletedAtUtc.AddMinutes(recordedDeletionOffsetMinutes)
            };
            var unitOfWork = new EducationUnitOfWorkStub();
            DeleteEducationSimulationCommandHandler handler = CreateHandler(repository, unitOfWork);

            DeleteEducationSimulationResult result = await handler.Handle(
                request: CreateCommand(DeletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: expectedStatus,
                actual: result.Status);
            Assert.Null(repository.DeletedHostId);
            Assert.Null(repository.RecordedDeletedAtUtc);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_NonUtcDeletionTimestamp_DoesNotOpenTransaction()
        {
            var repository = new DeletionRepositoryStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            DeleteEducationSimulationCommandHandler handler = CreateHandler(repository, unitOfWork);

            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
                request: CreateCommand(DeletedAtUtc.ToOffset(TimeSpan.FromHours(3))),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: 0,
                actual: unitOfWork.TransactionCount);
        }

        private static DeleteEducationSimulationCommandHandler CreateHandler(
            DeletionRepositoryStub repository,
            EducationUnitOfWorkStub unitOfWork)
        {
            return new DeleteEducationSimulationCommandHandler(
                deletionRepository: repository,
                unitOfWork: unitOfWork,
                timeProvider: new FixedTimeProvider(UpdatedAtUtc));
        }

        private static DeleteEducationSimulationCommand CreateCommand(DateTimeOffset deletedAtUtc)
        {
            return new DeleteEducationSimulationCommand(
                SimulationHostId: HostId,
                DeletedAtUtc: deletedAtUtc);
        }

        private sealed class DeletionRepositoryStub : IEducationSimulationDeletionRepository
        {
            public DateTimeOffset? RecordedDeletion { get; init; }
            public SimulationHostId? DeletedHostId { get; private set; }
            public DateTimeOffset? RecordedDeletedAtUtc { get; private set; }
            public DateTimeOffset? RecordedUpdatedAtUtc { get; private set; }

            public Task<DateTimeOffset?> GetDeletedAtUtcAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(RecordedDeletion);
            }

            public Task DeleteSimulationDataAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                DeletedHostId = simulationHostId;
                return Task.CompletedTask;
            }

            public Task RecordAsync(
                SimulationHostId simulationHostId,
                DateTimeOffset deletedAtUtc,
                DateTimeOffset updatedAtUtc,
                CancellationToken cancellationToken = default)
            {
                RecordedDeletedAtUtc = deletedAtUtc;
                RecordedUpdatedAtUtc = updatedAtUtc;
                return Task.CompletedTask;
            }
        }

        private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => value;
        }
    }
}
