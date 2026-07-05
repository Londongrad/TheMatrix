using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface ICareOperationalProfileProvider
{
    Task<CareOperationalProfile> GetAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default);
}
