namespace Matrix.CityCore.Contracts.Simulation.Views
{
    public sealed record SimulationClockView(
        Guid SimulationId,
        Guid HostId,
        string HostKind,
        string SimulationKind,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        string State);
}
