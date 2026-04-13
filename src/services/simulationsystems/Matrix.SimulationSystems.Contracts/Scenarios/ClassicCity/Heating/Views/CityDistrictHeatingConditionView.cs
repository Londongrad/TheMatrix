namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views
{
    public sealed record CityDistrictHeatingConditionView(
        Guid DistrictId,
        decimal HeatingCoverageIndex,
        decimal HeatingSupportIndex,
        decimal OutageRiskIndex,
        decimal ComfortStressIndex,
        decimal MaintenancePriorityIndex);
}
