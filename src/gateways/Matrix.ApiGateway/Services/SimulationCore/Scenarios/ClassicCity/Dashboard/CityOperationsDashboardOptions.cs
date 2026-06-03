namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    public sealed class CityOperationsDashboardOptions
    {
        public const string SectionName = "CityOperationsDashboard";

        public int PanelReadTimeoutSeconds { get; init; } = 4;

        public int HealthProbeTimeoutSeconds { get; init; } = 5;

        public int MaxConcurrentCitySnapshotLoads { get; init; } = 4;
    }
}
