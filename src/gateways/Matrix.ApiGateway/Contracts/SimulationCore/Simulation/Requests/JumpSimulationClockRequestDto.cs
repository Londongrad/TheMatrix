namespace Matrix.ApiGateway.Contracts.SimulationCore.Simulation.Requests
{
    public sealed class JumpSimulationClockRequestDto
    {
        public DateTimeOffset NewSimTimeUtc { get; init; }
    }
}
