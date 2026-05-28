using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence;

internal static class SimulationInstanceRepositoryExtensions
{
    public static async Task<SimulationInstance> GetRequiredByHostAsync(
        this ISimulationInstanceRepository repository,
        SimulationRuntimeKey runtimeKey,
        SimulationHostId hostId,
        CancellationToken cancellationToken)
    {
        SimulationInstance? instance = await repository.GetByHostAsync(
            runtimeKey: runtimeKey,
            hostId: hostId,
            cancellationToken: cancellationToken);

        return instance ?? throw new InvalidOperationException(
            $"Simulation instance for runtime '{runtimeKey}' and host '{hostId.Value}' was not found.");
    }
}
