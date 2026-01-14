namespace Matrix.ApiGateway.Configurations.Options
{
    public sealed class DownstreamServicesOptions
    {
        public const string SectionName = "DownstreamServices";

        public string CityCore { get; init; } = string.Empty;
        public string Economy { get; init; } = string.Empty;
        public string Population { get; init; } = string.Empty;
        public string Identity { get; init; } = string.Empty;
    }
}
