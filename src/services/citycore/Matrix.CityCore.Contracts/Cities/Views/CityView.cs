namespace Matrix.CityCore.Contracts.Cities.Views
{
    public sealed record CityView(
        Guid CityId,
        string Name,
        string SimulationKind,
        string Status,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string GenerationSeed,
        string SizeTier,
        string UrbanDensity,
        string DevelopmentLevel,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ArchivedAtUtc);
}
