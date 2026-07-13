using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;
using MediatR;

namespace Matrix.Education.Application.Progression.AdvanceEducationProgression
{
    public sealed class AdvanceEducationProgressionCommandHandler(
        IEducationProgressionCheckpointRepository checkpointRepository,
        IEducationSimulationDeletionRepository deletionRepository,
        EducationProgressionBatchProcessorRegistry batchProcessorRegistry,
        IEducationUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<AdvanceEducationProgressionCommand, AdvanceEducationProgressionResult>
    {
        public Task<AdvanceEducationProgressionResult> Handle(
            AdvanceEducationProgressionCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var runtimeKey = new SimulationRuntimeKey(
                scenarioKey: new SimulationScenarioKey(request.ScenarioKey),
                hostTypeKey: new SimulationHostTypeKey(request.HostTypeKey));
            EducationProgressionBatch batch = EducationProgressionBatch.Create(
                runtimeKey: runtimeKey,
                simulationHostId: simulationHostId,
                tickId: request.TickId,
                fromSimTimeUtc: request.FromSimTimeUtc,
                toSimTimeUtc: request.ToSimTimeUtc);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => AdvanceInsideTransactionAsync(batch, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<AdvanceEducationProgressionResult> AdvanceInsideTransactionAsync(
            EducationProgressionBatch batch,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
                simulationHostId: batch.SimulationHostId,
                cancellationToken: cancellationToken);

            if (deletedAtUtc is not null)
                return Skipped(AdvanceEducationProgressionStatus.SimulationDeleted);

            EducationProgressionCheckpoint? checkpoint = await checkpointRepository.GetAsync(
                simulationHostId: batch.SimulationHostId,
                cancellationToken: cancellationToken);

            if (checkpoint is not null)
            {
                ProgressionTickDisposition disposition = checkpoint.Classify(batch.TickId);

                if (disposition != ProgressionTickDisposition.Accepted)
                    return Skipped(Map(disposition));

                if (batch.ToSimTimeUtc < checkpoint.LastCompletedAtUtc)
                    return Skipped(AdvanceEducationProgressionStatus.OutOfOrder);
            }

            IEducationProgressionBatchProcessor batchProcessor =
                batchProcessorRegistry.Resolve(batch.RuntimeKey);
            EducationProgressionBatchResult batchResult = await batchProcessor.ProcessAsync(
                batch: batch,
                cancellationToken: cancellationToken);
            DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();

            if (checkpoint is null)
            {
                checkpoint = EducationProgressionCheckpoint.CreateCompleted(
                    simulationHostId: batch.SimulationHostId,
                    tickId: batch.TickId,
                    completedAtUtc: batch.ToSimTimeUtc,
                    updatedAtUtc: updatedAtUtc);
                await checkpointRepository.AddAsync(
                    checkpoint: checkpoint,
                    cancellationToken: cancellationToken);
            }
            else
            {
                checkpoint.MarkCompleted(
                    tickId: batch.TickId,
                    completedAtUtc: batch.ToSimTimeUtc,
                    updatedAtUtc: updatedAtUtc);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new AdvanceEducationProgressionResult(
                Status: AdvanceEducationProgressionStatus.Applied,
                BatchResult: batchResult);
        }

        private static AdvanceEducationProgressionResult Skipped(
            AdvanceEducationProgressionStatus status)
        {
            return new AdvanceEducationProgressionResult(
                Status: status,
                BatchResult: EducationProgressionBatchResult.Empty);
        }

        private static AdvanceEducationProgressionStatus Map(
            ProgressionTickDisposition disposition)
        {
            return disposition switch
            {
                ProgressionTickDisposition.Duplicate => AdvanceEducationProgressionStatus.Duplicate,
                ProgressionTickDisposition.OutOfOrder => AdvanceEducationProgressionStatus.OutOfOrder,
                _ => throw new ArgumentOutOfRangeException(nameof(disposition), disposition, null)
            };
        }
    }
}
