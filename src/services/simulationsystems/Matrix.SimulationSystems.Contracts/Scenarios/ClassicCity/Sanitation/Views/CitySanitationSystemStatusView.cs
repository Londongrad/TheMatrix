namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views
{
    public sealed record CitySanitationSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
