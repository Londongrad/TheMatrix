namespace Matrix.Resources.Infrastructure.Options
{
    public sealed class DownstreamServicesOptions
    {
        public const string SectionName = "DownstreamServices";

        public string Economy { get; init; } = string.Empty;
        public string SimulationCore { get; init; } = string.Empty;
    }
}
