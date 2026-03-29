namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests
{
    public sealed record DispatchCityUtilityIncidentResponseRequest(
        string Focus = "Balanced",
        string Intensity = "Standard",
        bool EmergencyOverride = false);
}
