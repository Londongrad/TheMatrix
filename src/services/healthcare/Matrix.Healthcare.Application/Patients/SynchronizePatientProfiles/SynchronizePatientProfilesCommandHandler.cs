using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles
{
    public sealed class SynchronizePatientProfilesCommandHandler(
        IPatientProfileRepository patientProfileRepository,
        IHealthcareUnitOfWork unitOfWork)
        : IRequestHandler<SynchronizePatientProfilesCommand, SynchronizePatientProfilesResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<SynchronizePatientProfilesResult> Handle(
            SynchronizePatientProfilesCommand request,
            CancellationToken cancellationToken)
        {
            PreparedBatch batch = Prepare(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => SynchronizeInsideTransactionAsync(batch, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<SynchronizePatientProfilesResult> SynchronizeInsideTransactionAsync(
            PreparedBatch batch,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PatientProfile> existingProfiles =
                await patientProfileRepository.GetByIdsAsync(
                    patientIds: batch.PatientIds,
                    cancellationToken: cancellationToken);
            Dictionary<PatientId, PatientProfile> profilesById = existingProfiles.ToDictionary(
                profile => profile.PatientId);
            var addedProfiles = new List<PatientProfile>();
            int updatedProfiles = 0;
            int ignoredProfiles = 0;

            foreach (PreparedProfileFact fact in batch.Facts)
            {
                if (!profilesById.TryGetValue(fact.PatientId, out PatientProfile? profile))
                {
                    profile = PatientProfile.Register(
                        patientId: fact.PatientId,
                        simulationHostId: batch.SimulationHostId,
                        birthDate: fact.BirthDate,
                        sex: fact.Sex,
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
                    sex: fact.Sex,
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
                await patientProfileRepository.AddRangeAsync(
                    profiles: addedProfiles,
                    cancellationToken: cancellationToken);

            if (addedProfiles.Count > 0 || updatedProfiles > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SynchronizePatientProfilesResult(
                AddedProfiles: addedProfiles.Count,
                UpdatedProfiles: updatedProfiles,
                IgnoredProfiles: ignoredProfiles);
        }

        private static PreparedBatch Prepare(SynchronizePatientProfilesCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Profiles);

            if (request.Profiles.Count == 0)
                throw new ArgumentException(
                    message: "A patient profile synchronization batch cannot be empty.",
                    paramName: nameof(request.Profiles));

            if (request.Profiles.Count > MaxBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request.Profiles),
                    message: $"A patient profile synchronization batch cannot exceed {MaxBatchSize} profiles.");

            if (request.SynchronizedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Profile synchronization timestamps must be expressed in UTC.",
                    paramName: nameof(request.SynchronizedAtUtc));

            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var patientIds = new HashSet<PatientId>();
            var facts = new PreparedProfileFact[request.Profiles.Count];

            for (int index = 0; index < request.Profiles.Count; index++)
            {
                SynchronizePatientProfileItem item = request.Profiles[index] ??
                                                     throw new ArgumentException(
                                                         message: "A synchronization batch cannot contain null profiles.",
                                                         paramName: nameof(request.Profiles));
                var patientId = new PatientId(item.PatientId);

                if (!patientIds.Add(patientId))
                    throw new ArgumentException(
                        message: $"Patient '{patientId}' occurs more than once in a synchronization batch.",
                        paramName: nameof(request.Profiles));

                if (!Enum.IsDefined(item.Sex))
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(request.Profiles),
                        message: "Patient sex must be a supported value.");

                if (item.SourceRevision < 0)
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(request.Profiles),
                        message: "Profile source revisions cannot be negative.");

                facts[index] = new PreparedProfileFact(
                    PatientId: patientId,
                    BirthDate: item.BirthDate,
                    Sex: item.Sex,
                    IsAlive: item.IsAlive,
                    IsActive: item.IsActive,
                    SourceRevision: item.SourceRevision);
            }

            return new PreparedBatch(
                SimulationHostId: simulationHostId,
                SynchronizedAtUtc: request.SynchronizedAtUtc,
                PatientIds: patientIds.ToArray(),
                Facts: facts);
        }

        private sealed record PreparedBatch(
            SimulationHostId SimulationHostId,
            DateTimeOffset SynchronizedAtUtc,
            IReadOnlyCollection<PatientId> PatientIds,
            IReadOnlyList<PreparedProfileFact> Facts);

        private sealed record PreparedProfileFact(
            PatientId PatientId,
            DateOnly BirthDate,
            PatientSex Sex,
            bool IsAlive,
            bool IsActive,
            long SourceRevision);
    }
}
