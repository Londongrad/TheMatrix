namespace Matrix.CityCore.Contracts.Requests
{
    public sealed record JumpClockRequest(DateTimeOffset NewSimTimeUtc);
}
