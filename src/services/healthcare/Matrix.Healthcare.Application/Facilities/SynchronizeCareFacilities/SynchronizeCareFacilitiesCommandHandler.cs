using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities
{
    public sealed class SynchronizeCareFacilitiesCommandHandler(
        ICareFacilityRepository careFacilityRepository,
        IHealthcareSimulationDeletionRepository deletionRepository,
        IHealthcareUnitOfWork unitOfWork)
        : IRequestHandler<SynchronizeCareFacilitiesCommand, SynchronizeCareFacilitiesResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<SynchronizeCareFacilitiesResult> Handle(
            SynchronizeCareFacilitiesCommand request,
            CancellationToken cancellationToken)
        {
            PreparedBatch batch = Prepare(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => SynchronizeInsideTransactionAsync(batch, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<SynchronizeCareFacilitiesResult> SynchronizeInsideTransactionAsync(
            PreparedBatch batch,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
                simulationHostId: batch.SimulationHostId,
                cancellationToken: cancellationToken);
            if (deletedAtUtc is not null)
                return new SynchronizeCareFacilitiesResult(
                    Status: SynchronizeCareFacilitiesStatus.SimulationDeleted,
                    AddedFacilities: 0,
                    UpdatedFacilities: 0,
                    IgnoredFacilities: batch.Facilities.Count);

            IReadOnlyList<CareFacility> existingFacilities =
                await careFacilityRepository.GetByIdsAsync(
                    facilityIds: batch.FacilityIds,
                    cancellationToken: cancellationToken);
            Dictionary<CareFacilityId, CareFacility> facilitiesById = existingFacilities.ToDictionary(
                facility => facility.CareFacilityId);
            var addedFacilities = new List<CareFacility>();
            int updatedFacilities = 0;
            int ignoredFacilities = 0;

            foreach (PreparedFacility prepared in batch.Facilities)
            {
                if (!facilitiesById.TryGetValue(prepared.FacilityId, out CareFacility? facility))
                {
                    addedFacilities.Add(CareFacility.Register(
                        id: prepared.FacilityId,
                        simulationHostId: batch.SimulationHostId,
                        name: prepared.Name,
                        kind: prepared.Kind,
                        locationAnchorId: prepared.LocationAnchorId,
                        dailyPatientCapacity: prepared.DailyPatientCapacity,
                        isActive: prepared.IsActive,
                        sourceRevision: batch.SourceRevision,
                        synchronizedAtUtc: batch.SynchronizedAtUtc));
                    continue;
                }

                bool changed = facility.TrySynchronizeProvisioning(
                    simulationHostId: batch.SimulationHostId,
                    name: prepared.Name,
                    kind: prepared.Kind,
                    locationAnchorId: prepared.LocationAnchorId,
                    dailyPatientCapacity: prepared.DailyPatientCapacity,
                    isActive: prepared.IsActive,
                    sourceRevision: batch.SourceRevision,
                    synchronizedAtUtc: batch.SynchronizedAtUtc);

                if (changed)
                    updatedFacilities++;
                else
                    ignoredFacilities++;
            }

            if (addedFacilities.Count > 0)
                await careFacilityRepository.AddRangeAsync(
                    facilities: addedFacilities,
                    cancellationToken: cancellationToken);

            if (addedFacilities.Count > 0 || updatedFacilities > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SynchronizeCareFacilitiesResult(
                Status: SynchronizeCareFacilitiesStatus.Applied,
                AddedFacilities: addedFacilities.Count,
                UpdatedFacilities: updatedFacilities,
                IgnoredFacilities: ignoredFacilities);
        }

        private static PreparedBatch Prepare(SynchronizeCareFacilitiesCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Facilities);

            if (request.Facilities.Count == 0)
                throw new ArgumentException(
                    message: "A care facility synchronization batch cannot be empty.",
                    paramName: nameof(request.Facilities));
            if (request.Facilities.Count > MaxBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request.Facilities),
                    message: $"A care facility synchronization batch cannot exceed {MaxBatchSize} facilities.");
            if (request.SourceRevision < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request.SourceRevision),
                    message: "Care facility source revisions cannot be negative.");
            if (request.SynchronizedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Care facility synchronization timestamps must be expressed in UTC.",
                    paramName: nameof(request.SynchronizedAtUtc));

            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var facilityIds = new HashSet<CareFacilityId>();
            var facilities = new PreparedFacility[request.Facilities.Count];

            for (int index = 0; index < request.Facilities.Count; index++)
            {
                SynchronizeCareFacilityItem item = request.Facilities[index] ??
                                                   throw new ArgumentException(
                                                       message: "A synchronization batch cannot contain null facilities.",
                                                       paramName: nameof(request.Facilities));
                var facilityId = new CareFacilityId(item.FacilityId);
                if (!facilityIds.Add(facilityId))
                    throw new ArgumentException(
                        message: $"Care facility '{facilityId}' occurs more than once in a synchronization batch.",
                        paramName: nameof(request.Facilities));

                facilities[index] = new PreparedFacility(
                    FacilityId: facilityId,
                    Name: PrepareName(item.Name),
                    Kind: new CareFacilityKindKey(item.Kind),
                    LocationAnchorId: item.LocationAnchorId.HasValue
                        ? new LocationAnchorId(item.LocationAnchorId.Value)
                        : null,
                    DailyPatientCapacity: PrepareCapacity(item.DailyPatientCapacity),
                    IsActive: item.IsActive);
            }

            return new PreparedBatch(
                SimulationHostId: simulationHostId,
                SourceRevision: request.SourceRevision,
                SynchronizedAtUtc: request.SynchronizedAtUtc,
                FacilityIds: facilityIds.ToArray(),
                Facilities: facilities);
        }

        private static string PrepareName(string? value)
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException(
                    message: "A care facility name is required.",
                    paramName: nameof(value))
                : value.Trim();

            return normalized.Length <= CareFacility.MaxNameLength
                ? normalized
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: $"Care facility names cannot exceed {CareFacility.MaxNameLength} characters.");
        }

        private static int PrepareCapacity(int value)
        {
            return value > 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Daily patient capacity must be positive.");
        }

        private sealed record PreparedBatch(
            SimulationHostId SimulationHostId,
            long SourceRevision,
            DateTimeOffset SynchronizedAtUtc,
            IReadOnlyCollection<CareFacilityId> FacilityIds,
            IReadOnlyList<PreparedFacility> Facilities);

        private sealed record PreparedFacility(
            CareFacilityId FacilityId,
            string Name,
            CareFacilityKindKey Kind,
            LocationAnchorId? LocationAnchorId,
            int DailyPatientCapacity,
            bool IsActive);
    }
}
