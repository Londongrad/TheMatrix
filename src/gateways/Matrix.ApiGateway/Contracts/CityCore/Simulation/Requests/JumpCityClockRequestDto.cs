namespace Matrix.ApiGateway.Contracts.CityCore.Simulation.Requests
{
    public sealed class JumpCityClockRequestDto
    {
        public DateTimeOffset NewSimTimeUtc { get; init; }
    }
}
