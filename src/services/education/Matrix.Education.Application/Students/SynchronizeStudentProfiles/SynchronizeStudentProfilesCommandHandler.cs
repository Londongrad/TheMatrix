using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using MediatR;

namespace Matrix.Education.Application.Students.SynchronizeStudentProfiles
{
    public sealed class SynchronizeStudentProfilesCommandHandler(
        IStudentProfileRepository studentProfileRepository,
        IEducationSimulationDeletionRepository deletionRepository,
        IEducationUnitOfWork unitOfWork)
        : IRequestHandler<SynchronizeStudentProfilesCommand, SynchronizeStudentProfilesResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<SynchronizeStudentProfilesResult> Handle(
            SynchronizeStudentProfilesCommand request,
            CancellationToken cancellationToken)
        {
            PreparedBatch batch = Prepare(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => SynchronizeInsideTransactionAsync(batch, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<SynchronizeStudentProfilesResult> SynchronizeInsideTransactionAsync(
            PreparedBatch batch,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
                simulationHostId: batch.SimulationHostId,
                cancellationToken: cancellationToken);

            if (deletedAtUtc is not null)
                return new SynchronizeStudentProfilesResult(
                    Status: SynchronizeStudentProfilesStatus.SimulationDeleted,
                    AddedProfiles: 0,
                    UpdatedProfiles: 0,
                    IgnoredProfiles: batch.Facts.Count);

            IReadOnlyList<StudentProfile> existingProfiles =
                await studentProfileRepository.GetByIdsAsync(
                    residentIds: batch.ResidentIds,
                    cancellationToken: cancellationToken);
            Dictionary<ResidentId, StudentProfile> profilesById = existingProfiles.ToDictionary(
                profile => profile.ResidentId);
            var addedProfiles = new List<StudentProfile>();
            int updatedProfiles = 0;
            int ignoredProfiles = 0;

            foreach (PreparedProfileFact fact in batch.Facts)
            {
                if (!profilesById.TryGetValue(fact.ResidentId, out StudentProfile? profile))
                {
                    profile = StudentProfile.Register(
                        residentId: fact.ResidentId,
                        simulationHostId: batch.SimulationHostId,
                        birthDate: fact.BirthDate,
                        isAlive: fact.IsAlive,
                        isActive: fact.IsActive,
                        sourceRevision: fact.SourceRevision,
                        synchronizedAtUtc: batch.SynchronizedAtUtc);
                    addedProfiles.Add(profile);
                    continue;
                }

                bool changed = profile.TrySynchronizeResidentFacts(
                    simulationHostId: batch.SimulationHostId,
                    birthDate: fact.BirthDate,
                    isAlive: fact.IsAlive,
                    isActive: fact.IsActive,
                    sourceRevision: fact.SourceRevision,
                    synchronizedAtUtc: batch.SynchronizedAtUtc);

                if (changed)
                    updatedProfiles++;
                else
                    ignoredProfiles++;
            }

            if (addedProfiles.Count > 0)
                await studentProfileRepository.AddRangeAsync(
                    profiles: addedProfiles,
                    cancellationToken: cancellationToken);

            if (addedProfiles.Count > 0 || updatedProfiles > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SynchronizeStudentProfilesResult(
                Status: SynchronizeStudentProfilesStatus.Applied,
                AddedProfiles: addedProfiles.Count,
                UpdatedProfiles: updatedProfiles,
                IgnoredProfiles: ignoredProfiles);
        }

        private static PreparedBatch Prepare(SynchronizeStudentProfilesCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Profiles);

            if (request.Profiles.Count == 0)
                throw new ArgumentException(
                    message: "A student profile synchronization batch cannot be empty.",
                    paramName: nameof(request.Profiles));

            if (request.Profiles.Count > MaxBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request.Profiles),
                    message: $"A student profile synchronization batch cannot exceed {MaxBatchSize} profiles.");

            if (request.SynchronizedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Profile synchronization timestamps must be expressed in UTC.",
                    paramName: nameof(request.SynchronizedAtUtc));

            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var residentIds = new HashSet<ResidentId>();
            var facts = new PreparedProfileFact[request.Profiles.Count];

            for (int index = 0; index < request.Profiles.Count; index++)
            {
                SynchronizeStudentProfileItem item = request.Profiles[index] ??
                                                     throw new ArgumentException(
                                                         message: "A synchronization batch cannot contain null profiles.",
                                                         paramName: nameof(request.Profiles));
                var residentId = new ResidentId(item.ResidentId);

                if (!residentIds.Add(residentId))
                    throw new ArgumentException(
                        message: $"Resident '{residentId}' occurs more than once in a synchronization batch.",
                        paramName: nameof(request.Profiles));

                if (item.SourceRevision < 0)
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(request.Profiles),
                        message: "Profile source revisions cannot be negative.");

                facts[index] = new PreparedProfileFact(
                    ResidentId: residentId,
                    BirthDate: item.BirthDate,
                    IsAlive: item.IsAlive,
                    IsActive: item.IsActive,
                    SourceRevision: item.SourceRevision);
            }

            return new PreparedBatch(
                SimulationHostId: simulationHostId,
                SynchronizedAtUtc: request.SynchronizedAtUtc,
                ResidentIds: residentIds.ToArray(),
                Facts: facts);
        }

        private sealed record PreparedBatch(
            SimulationHostId SimulationHostId,
            DateTimeOffset SynchronizedAtUtc,
            IReadOnlyCollection<ResidentId> ResidentIds,
            IReadOnlyList<PreparedProfileFact> Facts);

        private sealed record PreparedProfileFact(
            ResidentId ResidentId,
            DateOnly BirthDate,
            bool IsAlive,
            bool IsActive,
            long SourceRevision);
    }
}
