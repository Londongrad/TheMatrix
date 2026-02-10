namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class SyncCityEnvironmentRequest(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
