namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views
{
    public sealed record CityDistrictUtilityIncidentConditionView(
        Guid DistrictId,
        decimal UtilityContinuityIndex,
        decimal DispatchReadinessIndex,
        decimal IncidentPressureIndex,
        decimal CoordinationDifficultyIndex,
        decimal RestorationPriorityIndex);
}
