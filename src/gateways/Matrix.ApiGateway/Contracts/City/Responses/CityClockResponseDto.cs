namespace Matrix.ApiGateway.Contracts.City.Responses
{
    public sealed class CityClockResponseDto
    {
        public Guid CityId { get; init; }

        public DateTimeOffset SimTimeUtc { get; init; }

        public long TickId { get; init; }

        public decimal Speed { get; init; }

        public string State { get; init; } = string.Empty;
    }
}
