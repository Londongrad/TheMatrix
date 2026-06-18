namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events
{
    public sealed record CityEnvironmentV1(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
