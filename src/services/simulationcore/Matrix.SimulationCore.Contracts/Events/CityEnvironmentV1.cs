namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityEnvironmentV1(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
