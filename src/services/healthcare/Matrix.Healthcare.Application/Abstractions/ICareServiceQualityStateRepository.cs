using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface ICareServiceQualityStateRepository
{
    Task<CareServiceQualityState?> GetAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CareServiceQualityState state,
        CancellationToken cancellationToken = default);
}
