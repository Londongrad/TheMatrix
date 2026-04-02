namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions
{
    public sealed record CityEconomyBootstrapResult(
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol);

    public sealed record CityPopulationBootstrapEnvironment(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);

    public sealed record CityPopulationBootstrapTuning(
        int HousingPressurePercent,
        int EconomicStabilityPercent,
        int SocialVolatilityPercent,
        int FamilyFormationPercent);

    public sealed record ResidentialBuildingSeed(
        Guid ResidentialBuildingId,
        Guid DistrictId,
        int ResidentCapacity);

    public sealed record CityAnchorSeed(
        Guid CityAnchorId,
        Guid DistrictId,
        Guid AccessRoadNodeId,
        string Name,
        string Type,
        int Capacity,
        decimal PositionX,
        decimal PositionY,
        DateTimeOffset CreatedAtUtc);

    public sealed record CityPopulationBootstrapInitializationRequest(
        Guid CityId,
        DateOnly CurrentDate,
        DateTimeOffset CreatedAtUtc,
        int PeopleCount,
        int RandomSeed,
        CityPopulationBootstrapEnvironment Environment,
        CityPopulationBootstrapTuning Tuning,
        IReadOnlyCollection<CityAnchorSeed> CityAnchors,
        IReadOnlyCollection<ResidentialBuildingSeed> ResidentialBuildings);

    public sealed record CityPopulationBootstrapSummary(
        Guid CityId,
        int RequestedPeopleCount,
        int GeneratedPeopleCount,
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount,
        int HousedPeopleCount,
        int HomelessPeopleCount);
}
