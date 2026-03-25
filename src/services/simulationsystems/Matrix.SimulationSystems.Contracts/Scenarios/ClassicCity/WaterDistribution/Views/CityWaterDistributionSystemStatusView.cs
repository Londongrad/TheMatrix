namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views
{
    public sealed record CityWaterDistributionSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
