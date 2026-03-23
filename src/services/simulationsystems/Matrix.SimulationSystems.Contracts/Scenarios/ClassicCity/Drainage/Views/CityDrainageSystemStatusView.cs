namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Views
{
    public sealed record CityDrainageSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
