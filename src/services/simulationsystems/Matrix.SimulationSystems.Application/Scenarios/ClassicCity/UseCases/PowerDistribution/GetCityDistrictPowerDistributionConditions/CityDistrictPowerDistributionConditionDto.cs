namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityDistrictPowerDistributionConditions
{
    public sealed record CityDistrictPowerDistributionConditionDto(
        Guid DistrictId,
        decimal PowerCoverageIndex,
        decimal PowerSupportIndex,
        decimal OutageRiskIndex,
        decimal RestorationStrainIndex,
        decimal MaintenancePriorityIndex);
}
