using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Tests.TestSupport
{
    internal sealed class EducationInstitutionRepositoryStub(EducationInstitution? institution)
        : IEducationInstitutionRepository
    {
        internal int GetCallCount { get; private set; }

        public Task<EducationInstitution?> GetAsync(
            SimulationHostId simulationHostId,
            EducationInstitutionId institutionId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult(
                institution?.SimulationHostId == simulationHostId
                && institution.EducationInstitutionId == institutionId
                    ? institution
                    : null);
        }

        public Task<IReadOnlyList<EducationInstitution>> ListAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<EducationInstitution>> GetByIdsAsync(
            SimulationHostId simulationHostId,
            IReadOnlyCollection<EducationInstitutionId> institutionIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(
            EducationInstitution value,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddRangeAsync(
            IReadOnlyCollection<EducationInstitution> institutions,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
