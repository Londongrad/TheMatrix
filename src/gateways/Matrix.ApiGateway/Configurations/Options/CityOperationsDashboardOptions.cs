namespace Matrix.ApiGateway.Configurations.Options
{
    public sealed class CityOperationsDashboardOptions
    {
        public const string SectionName = "CityOperationsDashboard";

        public int PanelReadTimeoutSeconds { get; init; } = 4;

        public int HealthProbeTimeoutSeconds { get; init; } = 5;

        public int MaxConcurrentCitySnapshotLoads { get; init; } = 4;
    }
}
