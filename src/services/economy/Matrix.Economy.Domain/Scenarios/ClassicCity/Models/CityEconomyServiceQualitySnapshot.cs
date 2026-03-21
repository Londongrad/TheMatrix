namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyServiceQualitySnapshot(
        decimal HealthcareQualityIndex,
        decimal EducationQualityIndex,
        decimal HousingSupportIndex,
        DateTimeOffset EvaluatedAtUtc);
}
