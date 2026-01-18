namespace Matrix.ApiGateway.DownstreamClients.CityCore.Models
{
    public sealed class CityCoreJumpClockRequestDto
    {
        public DateTimeOffset NewSimTimeUtc { get; init; }
    }
}
