namespace Matrix.ApiGateway.DownstreamClients.CityCore.Models
{
    public sealed class CitySimulationTimeDto
    {
        public DateTimeOffset CurrentTimeUtc { get; init; }
        public int SimMinutesPerTick { get; init; }
        public bool IsPaused { get; init; }
    }
}
