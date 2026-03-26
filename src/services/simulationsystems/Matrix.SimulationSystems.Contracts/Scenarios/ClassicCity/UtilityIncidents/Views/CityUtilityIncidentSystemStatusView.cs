namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views
{
    public sealed record CityUtilityIncidentSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
