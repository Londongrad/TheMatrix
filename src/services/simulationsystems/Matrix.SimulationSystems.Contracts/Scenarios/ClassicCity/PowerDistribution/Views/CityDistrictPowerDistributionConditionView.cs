namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views
{
    public sealed record CityDistrictPowerDistributionConditionView(
        Guid DistrictId,
        decimal PowerCoverageIndex,
        decimal PowerSupportIndex,
        decimal OutageRiskIndex,
        decimal RestorationStrainIndex,
        decimal MaintenancePriorityIndex);
}
