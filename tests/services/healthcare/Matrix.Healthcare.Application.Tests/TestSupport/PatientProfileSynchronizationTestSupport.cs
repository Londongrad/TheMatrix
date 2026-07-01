using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Tests.TestSupport
{
    internal sealed class PatientProfileSynchronizationTestContext(
        IReadOnlyList<PatientProfile>? existingProfiles = null,
        DateTimeOffset? deletedAtUtc = null)
    {
        internal PatientProfileRepositoryStub Repository { get; } = new(existingProfiles);
        internal HealthcareSimulationDeletionRepositoryStub DeletionRepository { get; } =
            new(deletedAtUtc);
        internal HealthcareUnitOfWorkStub UnitOfWork { get; } = new();

        internal SynchronizePatientProfilesCommandHandler CreateHandler()
        {
            return new SynchronizePatientProfilesCommandHandler(
                patientProfileRepository: Repository,
                deletionRepository: DeletionRepository,
                unitOfWork: UnitOfWork);
        }
    }

    internal sealed class HealthcareSimulationDeletionRepositoryStub(
        DateTimeOffset? deletedAtUtc = null)
        : IHealthcareSimulationDeletionRepository
    {
        internal int GetCallCount { get; private set; }

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

    internal sealed class PatientProfileRepositoryStub(
        IReadOnlyList<PatientProfile>? existingProfiles = null)
        : IPatientProfileRepository
    {
        private readonly IReadOnlyList<PatientProfile> _existingProfiles =
            existingProfiles ?? Array.Empty<PatientProfile>();

        internal int GetCallCount { get; private set; }
        internal int AddRangeCallCount { get; private set; }
        internal IReadOnlyCollection<PatientId> RequestedIds { get; private set; } =
            Array.Empty<PatientId>();
        internal IReadOnlyCollection<PatientProfile> AddedProfiles { get; private set; } =
            Array.Empty<PatientProfile>();

        public Task<IReadOnlyList<PatientProfile>> GetByIdsAsync(
            IReadOnlyCollection<PatientId> patientIds,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            RequestedIds = patientIds;
            return Task.FromResult(_existingProfiles);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<PatientProfile> profiles,
            CancellationToken cancellationToken = default)
        {
            AddRangeCallCount++;
            AddedProfiles = profiles;
            return Task.CompletedTask;
        }
    }

    internal sealed class HealthcareUnitOfWorkStub : IHealthcareUnitOfWork
    {
        internal int SaveCount { get; private set; }
        internal int TransactionCount { get; private set; }
        internal IsolationLevel LastIsolationLevel { get; private set; }

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

    internal static class PatientProfileSynchronizationTestData
    {
        internal static readonly Guid HostId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        internal static readonly DateTimeOffset SynchronizedAtUtc =
            new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

        internal static PatientProfile CreateProfile(
            Guid patientId,
            long sourceRevision,
            Guid? simulationHostId = null)
        {
            return PatientProfile.Register(
                patientId: new PatientId(patientId),
                simulationHostId: new SimulationHostId(simulationHostId ?? HostId),
                birthDate: new DateOnly(2030, 5, 12),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: sourceRevision,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(-1));
        }

        internal static SynchronizePatientProfileItem CreateItem(
            Guid patientId,
            long sourceRevision,
            DateOnly? birthDate = null,
            PatientSex sex = PatientSex.Female,
            bool isAlive = true,
            bool isActive = true,
            long lifecycleRevision = 0)
        {
            return new SynchronizePatientProfileItem(
                PatientId: patientId,
                BirthDate: birthDate ?? new DateOnly(2030, 5, 12),
                Sex: sex,
                IsAlive: isAlive,
                IsActive: isActive,
                SourceRevision: sourceRevision,
                LifecycleRevision: lifecycleRevision);
        }

        internal static SynchronizePatientProfilesCommand CreateCommand(
            params SynchronizePatientProfileItem[] profiles)
        {
            return new SynchronizePatientProfilesCommand(
                SimulationHostId: HostId,
                SynchronizedAtUtc: SynchronizedAtUtc,
                Profiles: profiles);
        }
    }
}
