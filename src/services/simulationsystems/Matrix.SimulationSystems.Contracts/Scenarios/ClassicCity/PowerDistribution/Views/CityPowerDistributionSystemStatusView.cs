namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views
{
    public sealed record CityPowerDistributionSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
