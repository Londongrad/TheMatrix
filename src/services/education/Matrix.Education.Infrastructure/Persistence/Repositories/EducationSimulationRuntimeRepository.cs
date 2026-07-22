using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence.Models;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Infrastructure.Persistence.Repositories;

public sealed class EducationSimulationRuntimeRepository(EducationDbContext dbContext) : IEducationSimulationRuntimeRepository
{
    public async Task<SimulationRuntimeKey?> GetAsync(SimulationHostId hostId, CancellationToken cancellationToken = default)
    {
        var state = await dbContext.SimulationRuntimes.FindAsync([hostId], cancellationToken);
        return state?.ToRuntimeKey();
    }

    public async Task EnsureAsync(SimulationHostId hostId, SimulationRuntimeKey runtimeKey, CancellationToken cancellationToken = default)
    {
        if (runtimeKey.IsEmpty)
            throw new ArgumentException("An education runtime is required.", nameof(runtimeKey));
        SimulationRuntimeKey? existing = await GetAsync(hostId, cancellationToken);
        if (existing is { } recorded && recorded != runtimeKey)
            throw new InvalidOperationException($"Education simulation '{hostId}' cannot change runtime from '{recorded}' to '{runtimeKey}'.");
        if (existing is null)
            dbContext.SimulationRuntimes.Add(new EducationSimulationRuntimeState(hostId, runtimeKey));
    }
}
