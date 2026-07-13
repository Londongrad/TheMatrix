using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Abstractions
{
    public interface IStudentEnrollmentRepository
    {
        Task<StudentEnrollment?> GetActiveByResidentAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentEnrollment>> ListActiveAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            StudentEnrollment enrollment,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<StudentEnrollment> enrollments,
            CancellationToken cancellationToken = default);
    }
}
