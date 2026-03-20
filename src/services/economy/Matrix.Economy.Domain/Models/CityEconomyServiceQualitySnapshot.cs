namespace Matrix.Economy.Domain.Models
{
    public sealed record CityEconomyServiceQualitySnapshot(
        decimal HealthcareQualityIndex,
        decimal EducationQualityIndex,
        decimal HousingSupportIndex,
        DateTimeOffset EvaluatedAtUtc);
}
