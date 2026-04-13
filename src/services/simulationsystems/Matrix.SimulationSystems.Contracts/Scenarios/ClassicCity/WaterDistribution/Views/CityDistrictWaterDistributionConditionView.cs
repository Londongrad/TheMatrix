namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views
{
    public sealed record CityDistrictWaterDistributionConditionView(
        Guid DistrictId,
        decimal WaterCoverageIndex,
        decimal WaterSupportIndex,
        decimal DisruptionRiskIndex,
        decimal QualityRiskIndex,
        decimal MaintenancePriorityIndex);
}
