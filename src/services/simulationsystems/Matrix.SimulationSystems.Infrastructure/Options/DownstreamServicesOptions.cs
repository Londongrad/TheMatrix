namespace Matrix.SimulationSystems.Infrastructure.Options
{
    public sealed class DownstreamServicesOptions
    {
        public const string SectionName = "DownstreamServices";

        public string Economy { get; init; } = string.Empty;
    }
}
