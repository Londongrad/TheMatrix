using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Tests.TestSupport
{
    internal sealed class EducationStudentParticipationOutboxWriterStub
        : IEducationStudentParticipationOutboxWriter
    {
        internal List<EducationStudentParticipationBatchV1> Batches { get; } = [];

        public Task AddAsync(
            EducationStudentParticipationBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            Batches.Add(batch);
            return Task.CompletedTask;
        }
    }

    internal sealed class EducationFixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    internal sealed class EducationInstitutionRepositoryStub : IEducationInstitutionRepository
    {
        private readonly Dictionary<EducationInstitutionId, EducationInstitution> _institutions;

        internal EducationInstitutionRepositoryStub(EducationInstitution? institution)
            : this(institution is null ? [] : [institution])
        {
        }

        internal EducationInstitutionRepositoryStub(
            IReadOnlyCollection<EducationInstitution> institutions)
        {
            _institutions = institutions.ToDictionary(value => value.EducationInstitutionId);
        }

        internal int GetCallCount { get; private set; }
        internal int GetByIdsCallCount { get; private set; }
        internal int ListCallCount { get; private set; }
        internal int ListActiveCallCount { get; private set; }
        internal int AddRangeCallCount { get; private set; }
        internal List<EducationInstitution> Added { get; } = [];

        public Task<EducationInstitution?> GetAsync(
            SimulationHostId simulationHostId,
            EducationInstitutionId institutionId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            _institutions.TryGetValue(institutionId, out EducationInstitution? institution);
            return Task.FromResult(institution?.SimulationHostId == simulationHostId
                ? institution
                : null);
        }

        public Task<IReadOnlyList<EducationInstitution>> ListAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            IReadOnlyList<EducationInstitution> institutions = _institutions.Values
               .Where(institution => institution.SimulationHostId == simulationHostId)
               .OrderBy(institution => institution.Name)
               .ThenBy(institution => institution.EducationInstitutionId.Value)
               .ToArray();
            return Task.FromResult(institutions);
        }

        public Task<IReadOnlyList<EducationInstitution>> ListActiveAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            ListActiveCallCount++;
            IReadOnlyList<EducationInstitution> institutions = _institutions.Values
               .Where(institution => institution.SimulationHostId == simulationHostId
                                     && institution.IsActive)
               .OrderBy(institution => institution.Name)
               .ThenBy(institution => institution.EducationInstitutionId.Value)
               .ToArray();
            return Task.FromResult(institutions);
        }

        public Task<IReadOnlyList<EducationInstitution>> GetByIdsAsync(
            SimulationHostId simulationHostId,
            IReadOnlyCollection<EducationInstitutionId> institutionIds,
            CancellationToken cancellationToken = default)
        {
            GetByIdsCallCount++;
            IReadOnlyList<EducationInstitution> resolved = institutionIds
               .Where(_institutions.ContainsKey)
               .Select(id => _institutions[id])
               .Where(institution => institution.SimulationHostId == simulationHostId)
               .ToArray();
            return Task.FromResult(resolved);
        }

        public Task AddAsync(
            EducationInstitution value,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddRangeAsync(
            IReadOnlyCollection<EducationInstitution> institutions,
            CancellationToken cancellationToken = default)
        {
            AddRangeCallCount++;
            Added.AddRange(institutions);
            return Task.CompletedTask;
        }
    }

    internal sealed class StudentEnrollmentRepositoryStub(StudentEnrollment? active = null)
        : IStudentEnrollmentRepository
    {
        private readonly List<StudentEnrollment> _existing = active is null ? [] : [active];
        internal List<StudentEnrollment> Added { get; } = [];
        internal int GetActiveCallCount { get; private set; }

        public Task<StudentEnrollment?> GetActiveByResidentAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            CancellationToken cancellationToken = default)
        {
            GetActiveCallCount++;
            StudentEnrollment? enrollment = _existing
               .Concat(Added)
               .SingleOrDefault(value => value.SimulationHostId == simulationHostId &&
                                         value.ResidentId == residentId &&
                                         value.IsActive);
            return Task.FromResult(enrollment);
        }

        public Task<IReadOnlyList<StudentEnrollment>> ListActiveAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<StudentEnrollment> enrollments = _existing
               .Where(enrollment => enrollment.SimulationHostId == simulationHostId &&
                                    enrollment.IsActive)
               .OrderBy(enrollment => enrollment.EnrollmentId.Value)
               .ToArray();
            return Task.FromResult(enrollments);
        }

        public Task AddAsync(
            StudentEnrollment enrollment,
            CancellationToken cancellationToken = default)
        {
            Added.Add(enrollment);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<StudentEnrollment> enrollments,
            CancellationToken cancellationToken = default)
        {
            Added.AddRange(enrollments);
            return Task.CompletedTask;
        }
    }
}
