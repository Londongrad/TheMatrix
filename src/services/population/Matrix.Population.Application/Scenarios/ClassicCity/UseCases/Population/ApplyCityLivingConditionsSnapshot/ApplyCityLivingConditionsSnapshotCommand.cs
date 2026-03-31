using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot
{
    public sealed record ApplyCityLivingConditionsSnapshotCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        decimal FloodingIndex,
        decimal RoadAccessibilityIndex,
        decimal PowerCoverageIndex,
        decimal UtilityContinuityIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        decimal SanitationCoverageIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc)
        : IRequest<ApplyCityLivingConditionsSnapshotResult>;
}
