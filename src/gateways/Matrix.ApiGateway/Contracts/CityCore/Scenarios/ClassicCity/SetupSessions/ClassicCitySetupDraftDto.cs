namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed record ClassicCitySetupDraftDto(
        string Name,
        string StartSimTimeLocal,
        DateTimeOffset? StartSimTimeUtc,
        string SpeedMultiplier,
        string ClimateZone,
        string Hemisphere,
        string UtcOffsetMinutes,
        string GenerationSeed,
        string PopulationTargetMode = "Preset10K",
        string SizeTier = "Medium",
        string UrbanDensity = "Balanced",
        string DevelopmentLevel = "Balanced",
        string PopulationOccupancyProfile = "Balanced",
        string PlannedPeopleCount = "");
}
