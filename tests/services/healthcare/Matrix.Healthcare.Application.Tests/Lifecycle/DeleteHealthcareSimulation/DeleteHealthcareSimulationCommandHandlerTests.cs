using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Lifecycle.DeleteHealthcareSimulation;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Lifecycle.DeleteHealthcareSimulation
{
    public sealed class DeleteHealthcareSimulationCommandHandlerTests
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
            var unitOfWork = new HealthcareUnitOfWorkStub();
            DeleteHealthcareSimulationCommandHandler handler = CreateHandler(repository, unitOfWork);

            DeleteHealthcareSimulationResult result = await handler.Handle(
                request: CreateCommand(DeletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(DeleteHealthcareSimulationStatus.Applied, result.Status);
            Assert.Equal(new SimulationHostId(HostId), repository.DeletedHostId);
            Assert.Equal(DeletedAtUtc, repository.RecordedDeletedAtUtc);
            Assert.Equal(UpdatedAtUtc, repository.RecordedUpdatedAtUtc);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(System.Data.IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Theory]
        [InlineData(0, DeleteHealthcareSimulationStatus.Duplicate)]
        [InlineData(1, DeleteHealthcareSimulationStatus.Stale)]
        public async Task Handle_AlreadyObservedDeletion_SkipsMutation(
            int recordedDeletionOffsetMinutes,
            DeleteHealthcareSimulationStatus expectedStatus)
        {
            var repository = new DeletionRepositoryStub
            {
                RecordedDeletion = DeletedAtUtc.AddMinutes(recordedDeletionOffsetMinutes)
            };
            var unitOfWork = new HealthcareUnitOfWorkStub();
            DeleteHealthcareSimulationCommandHandler handler = CreateHandler(repository, unitOfWork);

            DeleteHealthcareSimulationResult result = await handler.Handle(
                request: CreateCommand(DeletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);
            Assert.Null(repository.DeletedHostId);
            Assert.Null(repository.RecordedDeletedAtUtc);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_NonUtcDeletionTimestamp_DoesNotOpenTransaction()
        {
            var repository = new DeletionRepositoryStub();
            var unitOfWork = new HealthcareUnitOfWorkStub();
            DeleteHealthcareSimulationCommandHandler handler = CreateHandler(repository, unitOfWork);

            await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
                request: CreateCommand(DeletedAtUtc.ToOffset(TimeSpan.FromHours(3))),
                cancellationToken: CancellationToken.None));

            Assert.Equal(0, unitOfWork.TransactionCount);
        }

        private static DeleteHealthcareSimulationCommandHandler CreateHandler(
            DeletionRepositoryStub repository,
            HealthcareUnitOfWorkStub unitOfWork)
        {
            return new DeleteHealthcareSimulationCommandHandler(
                deletionRepository: repository,
                unitOfWork: unitOfWork,
                timeProvider: new FixedTimeProvider(UpdatedAtUtc));
        }

        private static DeleteHealthcareSimulationCommand CreateCommand(DateTimeOffset deletedAtUtc)
        {
            return new DeleteHealthcareSimulationCommand(
                SimulationHostId: HostId,
                DeletedAtUtc: deletedAtUtc);
        }

        private sealed class DeletionRepositoryStub : IHealthcareSimulationDeletionRepository
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
