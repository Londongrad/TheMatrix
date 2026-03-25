namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Requests
{
    public sealed record DispatchCitySanitationMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard");
}
