namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.
    GetCityDistrictSanitationConditions
{
    public sealed record CityDistrictSanitationConditionDto(
        Guid DistrictId,
        decimal SanitationCoverageIndex,
        decimal SanitationSupportIndex,
        decimal OverflowRiskIndex,
        decimal ContaminationRiskIndex,
        decimal MaintenancePriorityIndex);
}
