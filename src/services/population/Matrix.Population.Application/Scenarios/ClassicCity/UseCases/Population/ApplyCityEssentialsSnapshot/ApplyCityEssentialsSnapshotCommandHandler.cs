using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot
{
    public sealed class ApplyCityEssentialsSnapshotCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEssentialsStateRepository cityPopulationEssentialsStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityEssentialsSnapshotCommand, ApplyCityEssentialsSnapshotResult>
    {
        public Task<ApplyCityEssentialsSnapshotResult> Handle(
            ApplyCityEssentialsSnapshotCommand request,
            CancellationToken cancellationToken)
        {
            string consumerName = request.ConsumerName;
            var cityId = CityId.From(request.CityId);
            DateTimeOffset effectiveAtUtc = request.EffectiveAtUtc.ToUniversalTime();

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: consumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ApplyCityEssentialsSnapshotResult(ApplyCityEssentialsSnapshotStatus.Duplicate);

                    if (await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityEssentialsSnapshotResult(ApplyCityEssentialsSnapshotStatus.CityDeleted);

                    if (await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityEssentialsSnapshotResult(ApplyCityEssentialsSnapshotStatus.CityArchived);

                    CityPopulationEssentialsState? state =
                        await cityPopulationEssentialsStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (state is not null &&
                        (request.EffectiveTickId < state.EffectiveTickId ||
                         (request.EffectiveTickId == state.EffectiveTickId &&
                          effectiveAtUtc <= state.EffectiveAtUtc)))
                        return new ApplyCityEssentialsSnapshotResult(ApplyCityEssentialsSnapshotStatus.Stale);

                    DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();

                    if (state is null)
                    {
                        state = CityPopulationEssentialsState.Create(
                            cityId: cityId,
                            supplyStressIndex: request.SupplyStressIndex,
                            emergencyRationingEnabled: request.EmergencyRationingEnabled,
                            foodStockLevelIndex: request.FoodStockLevelIndex,
                            foodShortageRiskIndex: request.FoodShortageRiskIndex,
                            medicineStockLevelIndex: request.MedicineStockLevelIndex,
                            medicineShortageRiskIndex: request.MedicineShortageRiskIndex,
                            emergencyWaterStockLevelIndex: request.EmergencyWaterStockLevelIndex,
                            emergencyWaterShortageRiskIndex: request.EmergencyWaterShortageRiskIndex,
                            effectiveTickId: request.EffectiveTickId,
                            effectiveAtUtc: effectiveAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationEssentialsStateRepository.AddAsync(
                            state: state,
                            cancellationToken: ct);
                    }
                    else
                        state.ApplySnapshot(
                            supplyStressIndex: request.SupplyStressIndex,
                            emergencyRationingEnabled: request.EmergencyRationingEnabled,
                            foodStockLevelIndex: request.FoodStockLevelIndex,
                            foodShortageRiskIndex: request.FoodShortageRiskIndex,
                            medicineStockLevelIndex: request.MedicineStockLevelIndex,
                            medicineShortageRiskIndex: request.MedicineShortageRiskIndex,
                            emergencyWaterStockLevelIndex: request.EmergencyWaterStockLevelIndex,
                            emergencyWaterShortageRiskIndex: request.EmergencyWaterShortageRiskIndex,
                            effectiveTickId: request.EffectiveTickId,
                            effectiveAtUtc: effectiveAtUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityEssentialsSnapshotResult(ApplyCityEssentialsSnapshotStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
