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
        string SizeTier,
        string UrbanDensity,
        string DevelopmentLevel,
        string PopulationMode = "automatic",
        string PlannedPeopleCount = "");
}
