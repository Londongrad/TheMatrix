namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions
{
    public sealed record CityDistrictHeatingConditionDto(
        Guid DistrictId,
        decimal HeatingCoverageIndex,
        decimal HeatingSupportIndex,
        decimal OutageRiskIndex,
        decimal ComfortStressIndex,
        decimal MaintenancePriorityIndex);
}
