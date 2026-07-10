using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Tests.TestSupport
{
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
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

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
        internal List<StudentEnrollment> Added { get; } = [];
        internal int GetActiveCallCount { get; private set; }

        public Task<StudentEnrollment?> GetActiveByResidentAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            CancellationToken cancellationToken = default)
        {
            GetActiveCallCount++;
            return Task.FromResult(active);
        }

        public Task<IReadOnlyList<StudentEnrollment>> ListActiveAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(
            StudentEnrollment enrollment,
            CancellationToken cancellationToken = default)
        {
            Added.Add(enrollment);
            return Task.CompletedTask;
        }
    }
}
