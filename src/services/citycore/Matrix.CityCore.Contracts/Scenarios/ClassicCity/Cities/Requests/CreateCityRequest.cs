namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Requests
{
    public sealed record CreateCityRequest(
        string Name,
        string? SimulationKind,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        string? GenerationSeed,
        string? SizeTier,
        string? UrbanDensity,
        string? DevelopmentLevel,
        string? PopulationOccupancyProfile,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m,
        int? PlannedPeopleCount = null,
        Guid? ProvisioningCorrelationId = null);
}
