namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Requests
{
    public sealed record DispatchCityHeatingMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard",
        bool EmergencyOverride = false);
}
