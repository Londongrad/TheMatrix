namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Requests
{
    public sealed record DispatchCityPowerDistributionMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard",
        bool EmergencyOverride = false);
}
