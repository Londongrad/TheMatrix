namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Requests
{
    public sealed record DispatchCityWaterDistributionMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard",
        bool EmergencyOverride = false);
}
