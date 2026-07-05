using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;

public sealed class SynchronizeCareMedicineSupplyCommandHandler(
    ICareMedicineSupplyStateRepository repository,
    IHealthcareSimulationDeletionRepository deletionRepository,
    IHealthcareUnitOfWork unitOfWork)
    : IRequestHandler<SynchronizeCareMedicineSupplyCommand, SynchronizeCareMedicineSupplyResult>
{
    public Task<SynchronizeCareMedicineSupplyResult> Handle(
        SynchronizeCareMedicineSupplyCommand request,
        CancellationToken cancellationToken)
    {
        PreparedObservation observation = Prepare(request);

        return unitOfWork.ExecuteInTransactionAsync(
            token => SynchronizeInsideTransactionAsync(observation, token),
            cancellationToken,
            IsolationLevel.Serializable);
    }

    private async Task<SynchronizeCareMedicineSupplyResult> SynchronizeInsideTransactionAsync(
        PreparedObservation observation,
        CancellationToken cancellationToken)
    {
        DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
            observation.SimulationHostId,
            cancellationToken);
        if (deletedAtUtc is not null)
            return new SynchronizeCareMedicineSupplyResult(
                SynchronizeCareMedicineSupplyStatus.SimulationDeleted,
                StateCreated: false,
                StateUpdated: false);

        CareMedicineSupplyState? state = await repository.GetAsync(
            observation.SimulationHostId,
            cancellationToken);
        bool created = false;
        bool updated = false;
        if (state is null)
        {
            state = CareMedicineSupplyState.Register(
                observation.SimulationHostId,
                observation.StockLevel,
                observation.ShortageRisk,
                observation.SourceRevision,
                observation.ObservedAtUtc);
            await repository.AddAsync(state, cancellationToken);
            created = true;
        }
        else
        {
            updated = state.TrySynchronize(
                observation.StockLevel,
                observation.ShortageRisk,
                observation.SourceRevision,
                observation.ObservedAtUtc);
        }

        if (created || updated)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SynchronizeCareMedicineSupplyResult(
            SynchronizeCareMedicineSupplyStatus.Applied,
            StateCreated: created,
            StateUpdated: updated);
    }

    private static PreparedObservation Prepare(SynchronizeCareMedicineSupplyCommand request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceRevision < 0)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(request.SourceRevision),
                message: "Medicine supply source revisions cannot be negative.");
        if (request.ObservedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException(
                message: "Medicine supply timestamps must be expressed in UTC.",
                paramName: nameof(request.ObservedAtUtc));

        return new PreparedObservation(
            new SimulationHostId(request.SimulationHostId),
            request.SourceRevision,
            new CareAvailabilityIndex(request.StockLevelIndex),
            new CareAvailabilityIndex(request.ShortageRiskIndex),
            request.ObservedAtUtc);
    }

    private sealed record PreparedObservation(
        SimulationHostId SimulationHostId,
        long SourceRevision,
        CareAvailabilityIndex StockLevel,
        CareAvailabilityIndex ShortageRisk,
        DateTimeOffset ObservedAtUtc);
}
