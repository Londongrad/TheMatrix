using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Lifecycle.DeleteHealthcareSimulation
{
    public sealed class DeleteHealthcareSimulationCommandHandler(
        IHealthcareSimulationDeletionRepository deletionRepository,
        IHealthcareUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<DeleteHealthcareSimulationCommand, DeleteHealthcareSimulationResult>
    {
        public Task<DeleteHealthcareSimulationResult> Handle(
            DeleteHealthcareSimulationCommand request,
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

        private async Task<DeleteHealthcareSimulationResult> DeleteInsideTransactionAsync(
            SimulationHostId simulationHostId,
            DateTimeOffset deletedAtUtc,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? recordedDeletion = await deletionRepository.GetDeletedAtUtcAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (recordedDeletion == deletedAtUtc)
                return new DeleteHealthcareSimulationResult(DeleteHealthcareSimulationStatus.Duplicate);

            if (recordedDeletion > deletedAtUtc)
                return new DeleteHealthcareSimulationResult(DeleteHealthcareSimulationStatus.Stale);

            await deletionRepository.DeleteSimulationDataAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);
            await deletionRepository.RecordAsync(
                simulationHostId: simulationHostId,
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: timeProvider.GetUtcNow(),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteHealthcareSimulationResult(DeleteHealthcareSimulationStatus.Applied);
        }
    }
}
