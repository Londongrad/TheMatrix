namespace Matrix.ApiGateway.Contracts.City.Requests
{
    public sealed class JumpCityClockRequestDto
    {
        public DateTimeOffset NewSimTimeUtc { get; init; }
    }
}
