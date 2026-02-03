namespace Matrix.Population.Contracts.Models
{
    public sealed record class SyncCityEnvironmentRequest(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
