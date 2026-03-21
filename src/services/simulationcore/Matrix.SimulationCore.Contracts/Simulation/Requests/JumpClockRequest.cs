namespace Matrix.SimulationCore.Contracts.Simulation.Requests
{
    public sealed record JumpClockRequest(DateTimeOffset NewSimTimeUtc);
}
