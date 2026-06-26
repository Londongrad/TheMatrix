using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions
{
    public interface IHealthcareSimulationDeletionRepository
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
