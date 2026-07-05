using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;

public sealed class SynchronizeCareServiceQualityCommandHandler(
    ICareServiceQualityStateRepository repository,
    IHealthcareSimulationDeletionRepository deletionRepository,
    IHealthcareUnitOfWork unitOfWork)
    : IRequestHandler<SynchronizeCareServiceQualityCommand, SynchronizeCareServiceQualityResult>
{
    public Task<SynchronizeCareServiceQualityResult> Handle(
        SynchronizeCareServiceQualityCommand request,
        CancellationToken cancellationToken)
    {
        PreparedObservation observation = Prepare(request);

        return unitOfWork.ExecuteInTransactionAsync(
            token => SynchronizeInsideTransactionAsync(observation, token),
            cancellationToken,
            IsolationLevel.Serializable);
    }

    private async Task<SynchronizeCareServiceQualityResult> SynchronizeInsideTransactionAsync(
        PreparedObservation observation,
        CancellationToken cancellationToken)
    {
        DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
            observation.SimulationHostId,
            cancellationToken);
        if (deletedAtUtc is not null)
            return new SynchronizeCareServiceQualityResult(
                SynchronizeCareServiceQualityStatus.SimulationDeleted,
                StateCreated: false,
                StateUpdated: false);

        CareServiceQualityState? state = await repository.GetAsync(
            observation.SimulationHostId,
            cancellationToken);
        bool created = false;
        bool updated = false;
        if (state is null)
        {
            state = CareServiceQualityState.Register(
                observation.SimulationHostId,
                observation.QualityMultiplier,
                observation.ObservedAtUtc);
            await repository.AddAsync(state, cancellationToken);
            created = true;
        }
        else
        {
            updated = state.TrySynchronize(
                observation.QualityMultiplier,
                observation.ObservedAtUtc);
        }

        if (created || updated)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SynchronizeCareServiceQualityResult(
            SynchronizeCareServiceQualityStatus.Applied,
            StateCreated: created,
            StateUpdated: updated);
    }

    private static PreparedObservation Prepare(SynchronizeCareServiceQualityCommand request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ObservedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException(
                message: "Care service quality timestamps must be expressed in UTC.",
                paramName: nameof(request.ObservedAtUtc));

        return new PreparedObservation(
            new SimulationHostId(request.SimulationHostId),
            new CareQualityMultiplier(request.QualityMultiplier),
            request.ObservedAtUtc);
    }

    private sealed record PreparedObservation(
        SimulationHostId SimulationHostId,
        CareQualityMultiplier QualityMultiplier,
        DateTimeOffset ObservedAtUtc);
}
