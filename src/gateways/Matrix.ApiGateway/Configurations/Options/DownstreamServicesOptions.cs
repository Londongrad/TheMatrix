namespace Matrix.ApiGateway.Configurations.Options
{
    public sealed class DownstreamServicesOptions
    {
        public const string SectionName = "DownstreamServices";

        public string SimulationCore { get; init; } = string.Empty;
        public string SimulationSystems { get; init; } = string.Empty;
        public string Economy { get; init; } = string.Empty;
        public string Resources { get; init; } = string.Empty;
        public string Population { get; init; } = string.Empty;
        public string Education { get; init; } = string.Empty;
        public string Identity { get; init; } = string.Empty;
    }
}
