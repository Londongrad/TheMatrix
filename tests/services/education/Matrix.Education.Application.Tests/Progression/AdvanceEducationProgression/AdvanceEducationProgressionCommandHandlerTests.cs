using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Progression;
using Matrix.Education.Application.Progression.AdvanceEducationProgression;
using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Application.Tests.Progression.AdvanceEducationProgression
{
    public sealed class AdvanceEducationProgressionCommandHandlerTests
    {
        private static readonly Guid HostId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly DateTimeOffset FromUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset ToUtc = FromUtc.AddHours(6);
        private static readonly DateTimeOffset UpdatedAtUtc = new(2048, 1, 1, 0, 0, 1, TimeSpan.Zero);

        [Fact]
        public async Task Handle_FirstTick_ProcessesOneBatchAndCreatesCheckpoint()
        {
            var repository = new CheckpointRepositoryStub();
            var processor = new BatchProcessorStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            AdvanceEducationProgressionCommandHandler handler = CreateHandler(
                repository,
                processor,
                unitOfWork);

            AdvanceEducationProgressionResult result = await handler.Handle(
                CreateCommand(tickId: 1),
                CancellationToken.None);

            Assert.Equal(AdvanceEducationProgressionStatus.Applied, result.Status);
            Assert.Same(processor.Result, result.BatchResult);
            Assert.Equal(1, processor.CallCount);
            Assert.Equal(1, repository.AddCount);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(1, unitOfWork.TransactionCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
            Assert.Equal(1, repository.Checkpoint!.LastCompletedTickId);
            Assert.Equal(ToUtc, repository.Checkpoint.LastCompletedAtUtc);
        }

        [Fact]
        public async Task Handle_NextTick_AdvancesExistingCheckpointWithoutAddingAnother()
        {
            EducationProgressionCheckpoint checkpoint = CreateCheckpoint(
                tickId: 1,
                completedAtUtc: ToUtc);
            var repository = new CheckpointRepositoryStub(checkpoint);
            var processor = new BatchProcessorStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            AdvanceEducationProgressionCommandHandler handler = CreateHandler(
                repository,
                processor,
                unitOfWork);

            AdvanceEducationProgressionResult result = await handler.Handle(
                CreateCommand(
                    tickId: 2,
                    fromUtc: ToUtc,
                    toUtc: ToUtc.AddHours(6)),
                CancellationToken.None);

            Assert.Equal(AdvanceEducationProgressionStatus.Applied, result.Status);
            Assert.Equal(1, processor.CallCount);
            Assert.Equal(0, repository.AddCount);
            Assert.Equal(2, checkpoint.LastCompletedTickId);
            Assert.Equal(ToUtc.AddHours(6), checkpoint.LastCompletedAtUtc);
        }

        [Theory]
        [InlineData(5, AdvanceEducationProgressionStatus.Duplicate)]
        [InlineData(4, AdvanceEducationProgressionStatus.OutOfOrder)]
        public async Task Handle_AlreadyObservedTick_SkipsBatchWork(
            long tickId,
            AdvanceEducationProgressionStatus expectedStatus)
        {
            var repository = new CheckpointRepositoryStub(CreateCheckpoint(
                tickId: 5,
                completedAtUtc: ToUtc));
            var processor = new BatchProcessorStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            AdvanceEducationProgressionCommandHandler handler = CreateHandler(
                repository,
                processor,
                unitOfWork);

            AdvanceEducationProgressionResult result = await handler.Handle(
                CreateCommand(tickId),
                CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);
            Assert.Same(EducationProgressionBatchResult.Empty, result.BatchResult);
            Assert.Equal(0, processor.CallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_SimulationTimeMovingBackwards_SkipsBatchWork()
        {
            var repository = new CheckpointRepositoryStub(CreateCheckpoint(
                tickId: 5,
                completedAtUtc: ToUtc.AddHours(1)));
            var processor = new BatchProcessorStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            AdvanceEducationProgressionCommandHandler handler = CreateHandler(
                repository,
                processor,
                unitOfWork);

            AdvanceEducationProgressionResult result = await handler.Handle(
                CreateCommand(tickId: 6),
                CancellationToken.None);

            Assert.Equal(AdvanceEducationProgressionStatus.OutOfOrder, result.Status);
            Assert.Equal(0, processor.CallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_SkipsBatchAndCheckpointWork()
        {
            var repository = new CheckpointRepositoryStub();
            var processor = new BatchProcessorStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            var deletionRepository = new DeletionRepositoryStub(ToUtc);
            AdvanceEducationProgressionCommandHandler handler = CreateHandler(
                repository,
                processor,
                unitOfWork,
                deletionRepository);

            AdvanceEducationProgressionResult result = await handler.Handle(
                CreateCommand(tickId: 6),
                CancellationToken.None);

            Assert.Equal(AdvanceEducationProgressionStatus.SimulationDeleted, result.Status);
            Assert.Same(EducationProgressionBatchResult.Empty, result.BatchResult);
            Assert.Equal(0, processor.CallCount);
            Assert.Equal(0, repository.AddCount);
            Assert.Equal(0, unitOfWork.SaveCount);
            Assert.Equal(1, deletionRepository.GetCallCount);
        }

        [Fact]
        public async Task Handle_InvalidBatch_DoesNotOpenTransaction()
        {
            var repository = new CheckpointRepositoryStub();
            var processor = new BatchProcessorStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            AdvanceEducationProgressionCommandHandler handler = CreateHandler(
                repository,
                processor,
                unitOfWork);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(
                CreateCommand(tickId: -1),
                CancellationToken.None));

            Assert.Equal(0, unitOfWork.TransactionCount);
            Assert.Equal(0, processor.CallCount);
        }

        private static AdvanceEducationProgressionCommandHandler CreateHandler(
            CheckpointRepositoryStub repository,
            BatchProcessorStub processor,
            EducationUnitOfWorkStub unitOfWork,
            DeletionRepositoryStub? deletionRepository = null)
        {
            return new AdvanceEducationProgressionCommandHandler(
                checkpointRepository: repository,
                deletionRepository: deletionRepository ?? new DeletionRepositoryStub(),
                batchProcessor: processor,
                unitOfWork: unitOfWork,
                timeProvider: new FixedTimeProvider(UpdatedAtUtc));
        }

        private static AdvanceEducationProgressionCommand CreateCommand(
            long tickId,
            DateTimeOffset? fromUtc = null,
            DateTimeOffset? toUtc = null)
        {
            return new AdvanceEducationProgressionCommand(
                SimulationHostId: HostId,
                TickId: tickId,
                FromSimTimeUtc: fromUtc ?? FromUtc,
                ToSimTimeUtc: toUtc ?? ToUtc);
        }

        private static EducationProgressionCheckpoint CreateCheckpoint(
            long tickId,
            DateTimeOffset completedAtUtc)
        {
            return EducationProgressionCheckpoint.CreateCompleted(
                simulationHostId: new SimulationHostId(HostId),
                tickId: tickId,
                completedAtUtc: completedAtUtc,
                updatedAtUtc: UpdatedAtUtc);
        }

        private sealed class CheckpointRepositoryStub(
            EducationProgressionCheckpoint? checkpoint = null)
            : IEducationProgressionCheckpointRepository
        {
            public EducationProgressionCheckpoint? Checkpoint { get; private set; } = checkpoint;
            public int AddCount { get; private set; }

            public Task<EducationProgressionCheckpoint?> GetAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Checkpoint);
            }

            public Task AddAsync(
                EducationProgressionCheckpoint checkpointToAdd,
                CancellationToken cancellationToken = default)
            {
                Checkpoint = checkpointToAdd;
                AddCount++;
                return Task.CompletedTask;
            }
        }

        private sealed class BatchProcessorStub : IEducationProgressionBatchProcessor
        {
            public EducationProgressionBatchResult Result { get; } = new(
                StudentProfilesEvaluated: 1000,
                EnrollmentsStarted: 20,
                EnrollmentsCompleted: 10,
                EnrollmentsWithdrawn: 2,
                InstitutionsUpdated: 4);

            public int CallCount { get; private set; }

            public Task<EducationProgressionBatchResult> ProcessAsync(
                EducationProgressionBatch batch,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(Result);
            }
        }

        private sealed class DeletionRepositoryStub(DateTimeOffset? deletedAtUtc = null)
            : IEducationSimulationDeletionRepository
        {
            public int GetCallCount { get; private set; }

            public Task<DateTimeOffset?> GetDeletedAtUtcAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                GetCallCount++;
                return Task.FromResult(deletedAtUtc);
            }

            public Task DeleteSimulationDataAsync(
                SimulationHostId simulationHostId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task RecordAsync(
                SimulationHostId simulationHostId,
                DateTimeOffset deletedAtUtcValue,
                DateTimeOffset updatedAtUtc,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class EducationUnitOfWorkStub : IEducationUnitOfWork
        {
            public int SaveCount { get; private set; }
            public int TransactionCount { get; private set; }
            public IsolationLevel LastIsolationLevel { get; private set; }

            public Task SaveChangesAsync(CancellationToken cancellationToken)
            {
                SaveCount++;
                return Task.CompletedTask;
            }

            public async Task ExecuteInTransactionAsync(
                Func<CancellationToken, Task> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                TransactionCount++;
                LastIsolationLevel = isolationLevel;
                await action(cancellationToken);
            }

            public async Task<T> ExecuteInTransactionAsync<T>(
                Func<CancellationToken, Task<T>> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                TransactionCount++;
                LastIsolationLevel = isolationLevel;
                return await action(cancellationToken);
            }
        }

        private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => value;
        }
    }
}
