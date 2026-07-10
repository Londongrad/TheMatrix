using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using MediatR;

namespace Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions
{
    public sealed class SynchronizeEducationInstitutionsCommandHandler(
        IEducationInstitutionRepository institutionRepository,
        IEducationSimulationDeletionRepository deletionRepository,
        IEducationUnitOfWork unitOfWork)
        : IRequestHandler<SynchronizeEducationInstitutionsCommand, SynchronizeEducationInstitutionsResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<SynchronizeEducationInstitutionsResult> Handle(
            SynchronizeEducationInstitutionsCommand request,
            CancellationToken cancellationToken)
        {
            PreparedRequest prepared = Prepare(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => SynchronizeInsideTransactionAsync(prepared, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<SynchronizeEducationInstitutionsResult> SynchronizeInsideTransactionAsync(
            PreparedRequest request,
            CancellationToken cancellationToken)
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(
                    request.SimulationHostId,
                    cancellationToken) is not null)
                return new SynchronizeEducationInstitutionsResult(
                    SynchronizeEducationInstitutionsStatus.SimulationDeleted,
                    0,
                    0,
                    request.Institutions.Count);

            EducationInstitutionId[] institutionIds = request.Institutions
               .Select(institution => institution.InstitutionId)
               .ToArray();
            Dictionary<EducationInstitutionId, EducationInstitution> existingById =
                (await institutionRepository.GetByIdsAsync(
                    request.SimulationHostId,
                    institutionIds,
                    cancellationToken)).ToDictionary(institution => institution.EducationInstitutionId);

            List<EducationInstitution> added = [];
            int updatedCount = 0;
            int ignoredCount = 0;
            foreach (PreparedInstitution input in request.Institutions)
            {
                if (!existingById.TryGetValue(input.InstitutionId, out EducationInstitution? institution))
                {
                    institution = EducationInstitution.Create(
                        id: input.InstitutionId,
                        simulationHostId: request.SimulationHostId,
                        name: input.Name,
                        kind: input.Kind,
                        capacity: input.Capacity,
                        locationAnchorId: input.LocationAnchorId);
                    institution.TrySynchronizeProvisioning(
                        sourceRevision: request.SourceRevision,
                        name: input.Name,
                        kind: input.Kind,
                        capacity: input.Capacity,
                        isActive: input.IsActive,
                        synchronizedAtUtc: request.SynchronizedAtUtc,
                        locationAnchorId: input.LocationAnchorId);
                    added.Add(institution);
                    continue;
                }

                bool updated = institution.TrySynchronizeProvisioning(
                    sourceRevision: request.SourceRevision,
                    name: input.Name,
                    kind: input.Kind,
                    capacity: input.Capacity,
                    isActive: input.IsActive,
                    synchronizedAtUtc: request.SynchronizedAtUtc,
                    locationAnchorId: input.LocationAnchorId);
                if (updated)
                    updatedCount++;
                else
                    ignoredCount++;
            }

            if (added.Count > 0)
                await institutionRepository.AddRangeAsync(added, cancellationToken);
            if (added.Count > 0 || updatedCount > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SynchronizeEducationInstitutionsResult(
                SynchronizeEducationInstitutionsStatus.Applied,
                added.Count,
                updatedCount,
                ignoredCount);
        }

        private static PreparedRequest Prepare(SynchronizeEducationInstitutionsCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.SourceRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(request));
            if (request.SynchronizedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    "Institution synchronization timestamps must be expressed in UTC.",
                    nameof(request));
            ArgumentNullException.ThrowIfNull(request.Institutions);
            if (request.Institutions.Count > MaxBatchSize)
                throw new ArgumentException(
                    $"Institution synchronization batches cannot exceed {MaxBatchSize} items.",
                    nameof(request));
            if (request.Institutions.Select(item => item.InstitutionId).Distinct().Count()
                != request.Institutions.Count)
                throw new ArgumentException(
                    "Institution synchronization batches cannot contain duplicate identifiers.",
                    nameof(request));

            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            PreparedInstitution[] institutions = request.Institutions
               .Select(item => new PreparedInstitution(
                    InstitutionId: new EducationInstitutionId(item.InstitutionId),
                    Name: item.Name,
                    Kind: new EducationInstitutionKindKey(item.Kind),
                    Capacity: item.Capacity > 0
                        ? item.Capacity
                        : throw new ArgumentOutOfRangeException(nameof(request)),
                    IsActive: item.IsActive,
                    LocationAnchorId: item.LocationAnchorId.HasValue
                        ? new LocationAnchorId(item.LocationAnchorId.Value)
                        : null))
               .ToArray();

            return new PreparedRequest(
                simulationHostId,
                request.SourceRevision,
                request.SynchronizedAtUtc,
                institutions);
        }

        private sealed record PreparedRequest(
            SimulationHostId SimulationHostId,
            long SourceRevision,
            DateTimeOffset SynchronizedAtUtc,
            IReadOnlyList<PreparedInstitution> Institutions);

        private sealed record PreparedInstitution(
            EducationInstitutionId InstitutionId,
            string Name,
            EducationInstitutionKindKey Kind,
            int Capacity,
            bool IsActive,
            LocationAnchorId? LocationAnchorId);
    }
}
