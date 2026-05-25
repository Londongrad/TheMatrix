namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions
{
    public sealed record CityDistrictUtilityIncidentConditionDto(
        Guid DistrictId,
        decimal UtilityContinuityIndex,
        decimal DispatchReadinessIndex,
        decimal IncidentPressureIndex,
        decimal CoordinationDifficultyIndex,
        decimal RestorationPriorityIndex);
}
