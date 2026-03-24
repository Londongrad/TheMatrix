namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Requests
{
    public sealed record DispatchCitySnowRemovalMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard");
}
