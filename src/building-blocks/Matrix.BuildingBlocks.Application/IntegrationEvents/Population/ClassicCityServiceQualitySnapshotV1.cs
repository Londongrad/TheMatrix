namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Population
{
    public sealed record ClassicCityServiceQualitySnapshotV1(
        Guid CityId,
        decimal HealthcareQualityIndex,
        decimal EducationQualityIndex,
        decimal HousingSupportIndex,
        DateTimeOffset OccurredAtUtc);
}
