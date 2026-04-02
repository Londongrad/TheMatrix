namespace Matrix.SimulationCore.Infrastructure.Options
{
    public sealed class DownstreamServicesOptions
    {
        public const string SectionName = "DownstreamServices";

        public string Economy { get; init; } = string.Empty;
        public string Population { get; init; } = string.Empty;
        public string SimulationSystems { get; init; } = string.Empty;
    }
}
