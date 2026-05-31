namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityView(
        Guid CityId,
        Guid SimulationId,
        string Name,
        string Status,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string GenerationSeed,
        CityRunMetadataView RunMetadata,
        string SizeTier,
        string UrbanDensity,
        string DevelopmentLevel,
        string EconomyProfile,
        string PopulationOccupancyProfile,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ArchivedAtUtc,
        int? PlannedPeopleCount = null);
}
