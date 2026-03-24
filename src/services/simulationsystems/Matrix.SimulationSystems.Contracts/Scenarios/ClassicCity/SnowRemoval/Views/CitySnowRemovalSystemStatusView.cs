namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Views
{
    public sealed record CitySnowRemovalSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
