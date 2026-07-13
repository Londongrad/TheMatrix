using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Abstractions
{
    public interface IStudentProfileRepository
    {
        Task<IReadOnlyList<StudentProfile>> ListBySimulationHostAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentProfile>> GetByIdsAsync(
            IReadOnlyCollection<ResidentId> residentIds,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<StudentProfile> profiles,
            CancellationToken cancellationToken = default);
    }
}
