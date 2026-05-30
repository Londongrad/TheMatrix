namespace Matrix.SimulationCore.Contracts.Simulation.Views
{
    public sealed record SimulationClockView(
        Guid SimulationId,
        Guid HostId,
        string ScenarioKey,
        string HostTypeKey,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        string State);
}
