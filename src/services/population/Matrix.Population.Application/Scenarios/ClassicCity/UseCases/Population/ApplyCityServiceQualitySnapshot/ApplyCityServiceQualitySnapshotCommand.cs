using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot
{
    public sealed record ApplyCityServiceQualitySnapshotCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        decimal HealthcareQualityIndex,
        decimal EducationQualityIndex,
        decimal HousingSupportIndex,
        DateTimeOffset OccurredAtUtc)
        : IRequest<ApplyCityServiceQualitySnapshotResult>;
}
