using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Tests.TestSupport
{
    internal sealed class StudentProfileSynchronizationTestContext(
        IReadOnlyList<StudentProfile>? existingProfiles = null,
        DateTimeOffset? deletedAtUtc = null)
    {
        internal StudentProfileRepositoryStub Repository { get; } = new(existingProfiles);
        internal EducationSimulationDeletionRepositoryStub DeletionRepository { get; } = new(deletedAtUtc);
        internal EducationUnitOfWorkStub UnitOfWork { get; } = new();

        internal SynchronizeStudentProfilesCommandHandler CreateHandler()
        {
            return new SynchronizeStudentProfilesCommandHandler(
                studentProfileRepository: Repository,
                deletionRepository: DeletionRepository,
                unitOfWork: UnitOfWork);
        }
    }

    internal sealed class EducationSimulationDeletionRepositoryStub(DateTimeOffset? deletedAtUtc = null)
        : IEducationSimulationDeletionRepository
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

    internal sealed class StudentProfileRepositoryStub(
        IReadOnlyList<StudentProfile>? existingProfiles = null)
        : IStudentProfileRepository
    {
        private readonly IReadOnlyList<StudentProfile> _existingProfiles =
            existingProfiles ?? Array.Empty<StudentProfile>();

        internal int GetCallCount { get; private set; }
        internal int AddRangeCallCount { get; private set; }
        internal IReadOnlyCollection<ResidentId> RequestedIds { get; private set; } =
            Array.Empty<ResidentId>();
        internal IReadOnlyCollection<StudentProfile> AddedProfiles { get; private set; } =
            Array.Empty<StudentProfile>();

        public Task<IReadOnlyList<StudentProfile>> GetByIdsAsync(
            IReadOnlyCollection<ResidentId> residentIds,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            RequestedIds = residentIds;
            return Task.FromResult(_existingProfiles);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<StudentProfile> profiles,
            CancellationToken cancellationToken = default)
        {
            AddRangeCallCount++;
            AddedProfiles = profiles;
            return Task.CompletedTask;
        }
    }

    internal sealed class EducationUnitOfWorkStub : IEducationUnitOfWork
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

    internal static class StudentProfileSynchronizationTestData
    {
        internal static readonly Guid HostId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        internal static readonly DateTimeOffset SynchronizedAtUtc =
            new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

        internal static StudentProfile CreateProfile(
            Guid residentId,
            long sourceRevision,
            Guid? simulationHostId = null)
        {
            return StudentProfile.Register(
                residentId: new ResidentId(residentId),
                simulationHostId: new SimulationHostId(simulationHostId ?? HostId),
                birthDate: new DateOnly(2030, 5, 12),
                isAlive: true,
                isActive: true,
                sourceRevision: sourceRevision,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(-1));
        }

        internal static SynchronizeStudentProfileItem CreateItem(
            Guid residentId,
            long sourceRevision,
            DateOnly? birthDate = null,
            bool isAlive = true,
            bool isActive = true,
            long lifecycleRevision = 0)
        {
            return new SynchronizeStudentProfileItem(
                ResidentId: residentId,
                BirthDate: birthDate ?? new DateOnly(2030, 5, 12),
                IsAlive: isAlive,
                IsActive: isActive,
                SourceRevision: sourceRevision,
                LifecycleRevision: lifecycleRevision);
        }

        internal static SynchronizeStudentProfilesCommand CreateCommand(
            params SynchronizeStudentProfileItem[] profiles)
        {
            return new SynchronizeStudentProfilesCommand(
                SimulationHostId: HostId,
                SynchronizedAtUtc: SynchronizedAtUtc,
                Profiles: profiles);
        }
    }
}
