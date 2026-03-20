using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot
{
    public sealed record ApplyCityCostOfLivingSnapshotCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        decimal WageMultiplier,
        decimal RetailPriceMultiplier,
        decimal HousingCostMultiplier,
        decimal UtilityCostMultiplier,
        decimal CostOfLivingIndex,
        decimal AffordabilityIndex,
        DateTimeOffset OccurredAtUtc)
        : IRequest<ApplyCityCostOfLivingSnapshotResult>;
}
