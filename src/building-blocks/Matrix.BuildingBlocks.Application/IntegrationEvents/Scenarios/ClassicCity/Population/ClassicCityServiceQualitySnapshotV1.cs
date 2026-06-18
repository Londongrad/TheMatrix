namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population
{
    public sealed record ClassicCityServiceQualitySnapshotV1(
        Guid CityId,
        decimal HealthcareQualityIndex,
        decimal EducationQualityIndex,
        decimal HousingSupportIndex,
        DateTimeOffset OccurredAtUtc);
}
