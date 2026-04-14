namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views
{
    public sealed record CityDistrictSanitationConditionView(
        Guid DistrictId,
        decimal SanitationCoverageIndex,
        decimal SanitationSupportIndex,
        decimal OverflowRiskIndex,
        decimal ContaminationRiskIndex,
        decimal MaintenancePriorityIndex);
}
