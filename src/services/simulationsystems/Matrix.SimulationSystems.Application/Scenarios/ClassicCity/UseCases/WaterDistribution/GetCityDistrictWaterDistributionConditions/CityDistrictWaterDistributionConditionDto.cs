namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityDistrictWaterDistributionConditions
{
    public sealed record CityDistrictWaterDistributionConditionDto(
        Guid DistrictId,
        decimal WaterCoverageIndex,
        decimal WaterSupportIndex,
        decimal DisruptionRiskIndex,
        decimal QualityRiskIndex,
        decimal MaintenancePriorityIndex);
}
