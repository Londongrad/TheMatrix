using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using MediatR;

namespace Matrix.Education.Application.Lifecycle.DeleteEducationSimulation
{
    public sealed class DeleteEducationSimulationCommandHandler(
        IEducationSimulationDeletionRepository deletionRepository,
        IEducationUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<DeleteEducationSimulationCommand, DeleteEducationSimulationResult>
    {
        public Task<DeleteEducationSimulationResult> Handle(
            DeleteEducationSimulationCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var simulationHostId = new SimulationHostId(request.SimulationHostId);

            if (request.DeletedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Simulation deletion timestamps must be expressed in UTC.",
                    paramName: nameof(request));

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => DeleteInsideTransactionAsync(
                    simulationHostId: simulationHostId,
                    deletedAtUtc: request.DeletedAtUtc,
                    cancellationToken: token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<DeleteEducationSimulationResult> DeleteInsideTransactionAsync(
            SimulationHostId simulationHostId,
            DateTimeOffset deletedAtUtc,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? recordedDeletion = await deletionRepository.GetDeletedAtUtcAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (recordedDeletion == deletedAtUtc)
                return new DeleteEducationSimulationResult(DeleteEducationSimulationStatus.Duplicate);

            if (recordedDeletion > deletedAtUtc)
                return new DeleteEducationSimulationResult(DeleteEducationSimulationStatus.Stale);

            await deletionRepository.DeleteSimulationDataAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);
            await deletionRepository.RecordAsync(
                simulationHostId: simulationHostId,
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: timeProvider.GetUtcNow(),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteEducationSimulationResult(DeleteEducationSimulationStatus.Applied);
        }
    }
}
