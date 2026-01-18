namespace Matrix.ApiGateway.DownstreamClients.CityCore.Models
{
    public sealed class CityCoreClockResponseDto
    {
        public Guid CityId { get; init; }

        public DateTimeOffset SimTimeUtc { get; init; }

        public long TickId { get; init; }

        public decimal Speed { get; init; }

        public int State { get; init; }
    }
}
