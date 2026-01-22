namespace Matrix.CityCore.Contracts.Simulation.Requests
{
    public sealed record JumpClockRequest(DateTimeOffset NewSimTimeUtc);
}
