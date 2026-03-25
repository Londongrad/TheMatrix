namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views
{
    public sealed record CityHeatingSystemStatusView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
