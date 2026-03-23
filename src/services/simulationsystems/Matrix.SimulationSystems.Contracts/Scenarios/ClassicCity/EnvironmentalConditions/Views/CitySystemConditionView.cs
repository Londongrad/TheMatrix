namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views
{
    public sealed record CitySystemConditionView(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex);
}
