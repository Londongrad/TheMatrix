using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Application.Abstractions
{
    public interface IEducationInstitutionRepository
    {
        Task<EducationInstitution?> GetAsync(
            SimulationHostId simulationHostId,
            EducationInstitutionId institutionId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EducationInstitution>> ListAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            EducationInstitution institution,
            CancellationToken cancellationToken = default);
    }
}
