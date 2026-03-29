namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Requests
{
    public sealed record DispatchCityDrainageMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard",
        bool EmergencyOverride = false);
}
