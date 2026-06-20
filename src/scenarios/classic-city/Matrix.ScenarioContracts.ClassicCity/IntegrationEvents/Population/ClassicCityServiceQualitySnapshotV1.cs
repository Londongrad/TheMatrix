namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population
{
    public sealed record ClassicCityServiceQualitySnapshotV1(
        Guid CityId,
        decimal HealthcareQualityIndex,
        decimal EducationQualityIndex,
        decimal HousingSupportIndex,
        DateTimeOffset OccurredAtUtc);
}
