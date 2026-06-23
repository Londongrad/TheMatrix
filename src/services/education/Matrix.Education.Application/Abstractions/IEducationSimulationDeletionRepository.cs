using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Application.Abstractions
{
    public interface IEducationSimulationDeletionRepository
    {
        Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task DeleteSimulationDataAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task RecordAsync(
            SimulationHostId simulationHostId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken = default);
    }
}
