namespace Matrix.ApiGateway.Contracts.CityCore.Simulation.Requests
{
    public sealed class JumpSimulationClockRequestDto
    {
        public DateTimeOffset NewSimTimeUtc { get; init; }
    }
}
