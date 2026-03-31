namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Population
{
    public sealed record ClassicCityLivingConditionsSnapshotV1(
        Guid CityId,
        decimal FloodingIndex,
        decimal RoadAccessibilityIndex,
        decimal PowerCoverageIndex,
        decimal UtilityContinuityIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        decimal SanitationCoverageIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc,
        DateTimeOffset OccurredAtUtc);
}
