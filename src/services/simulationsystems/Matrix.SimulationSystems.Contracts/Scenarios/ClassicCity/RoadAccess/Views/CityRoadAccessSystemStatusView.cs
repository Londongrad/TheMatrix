namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views
{
    public sealed record CityRoadAccessSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
