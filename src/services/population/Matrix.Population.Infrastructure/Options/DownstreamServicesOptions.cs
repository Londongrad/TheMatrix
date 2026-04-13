namespace Matrix.Population.Infrastructure.Options
{
    public sealed class DownstreamServicesOptions
    {
        public const string SectionName = "DownstreamServices";

        public string SimulationCore { get; init; } = string.Empty;
    }
}
