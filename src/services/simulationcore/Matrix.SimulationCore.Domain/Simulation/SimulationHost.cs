using Matrix.Simulation.Primitives;

namespace Matrix.SimulationCore.Domain.Simulation
{
    public sealed record SimulationHost(
        SimulationId SimulationId,
        SimulationHostId HostId,
        SimulationRuntimeKey RuntimeKey,
        SimulationHostKind HostKind,
        SimulationKind SimulationKind,
        SimulationHostState State,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ArchivedAtUtc)
    {
        public bool IsActive => State == SimulationHostState.Active;
        public bool IsArchived => State == SimulationHostState.Archived;
    }
}
