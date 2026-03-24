namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Requests
{
    public sealed record DispatchCityRoadAccessMaintenanceRequest(
        string Focus = "Balanced",
        string Intensity = "Standard");
}
