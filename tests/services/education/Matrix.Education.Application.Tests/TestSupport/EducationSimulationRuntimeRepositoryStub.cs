using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Tests.TestSupport;

internal sealed class EducationSimulationRuntimeRepositoryStub : IEducationSimulationRuntimeRepository
{
    public Dictionary<SimulationHostId, SimulationRuntimeKey> Runtimes { get; } = [];
    public int ReadCount { get; private set; }

    public Task<SimulationRuntimeKey?> GetAsync(SimulationHostId hostId, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult<SimulationRuntimeKey?>(Runtimes.TryGetValue(hostId, out var value) ? value : null);
    }

    public Task EnsureAsync(SimulationHostId hostId, SimulationRuntimeKey runtimeKey, CancellationToken cancellationToken = default)
    {
        if (Runtimes.TryGetValue(hostId, out var value) && value != runtimeKey)
            throw new InvalidOperationException();
        Runtimes[hostId] = runtimeKey;
        return Task.CompletedTask;
    }
}
