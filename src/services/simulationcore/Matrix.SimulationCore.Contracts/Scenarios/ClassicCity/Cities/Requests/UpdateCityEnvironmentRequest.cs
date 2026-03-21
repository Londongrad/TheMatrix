namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests
{
    public sealed record UpdateCityEnvironmentRequest(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
