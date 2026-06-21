using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Application.Abstractions
{
    public interface IEducationProgressionCheckpointRepository
    {
        Task<EducationProgressionCheckpoint?> GetAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            EducationProgressionCheckpoint checkpoint,
            CancellationToken cancellationToken = default);
    }
}
