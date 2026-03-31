using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot
{
    public sealed record ApplyCityEssentialsSnapshotCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled,
        decimal FoodStockLevelIndex,
        decimal FoodShortageRiskIndex,
        decimal MedicineStockLevelIndex,
        decimal MedicineShortageRiskIndex,
        decimal EmergencyWaterStockLevelIndex,
        decimal EmergencyWaterShortageRiskIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc)
        : IRequest<ApplyCityEssentialsSnapshotResult>;
}
