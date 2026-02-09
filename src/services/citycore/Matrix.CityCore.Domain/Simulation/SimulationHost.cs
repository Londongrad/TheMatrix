namespace Matrix.CityCore.Domain.Simulation
{
    public sealed record SimulationHost(
        SimulationId SimulationId,
        SimulationHostId HostId,
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
