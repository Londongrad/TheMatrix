using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot
{
    public sealed class ApplyCityCostOfLivingSnapshotCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationCostOfLivingStateRepository cityPopulationCostOfLivingStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityCostOfLivingSnapshotCommand, ApplyCityCostOfLivingSnapshotResult>
    {
        public Task<ApplyCityCostOfLivingSnapshotResult> Handle(
            ApplyCityCostOfLivingSnapshotCommand request,
            CancellationToken cancellationToken)
        {
            string consumerName = request.ConsumerName;
            var cityId = CityId.From(request.CityId);
            DateTimeOffset occurredAtUtc = request.OccurredAtUtc.ToUniversalTime();

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: consumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ApplyCityCostOfLivingSnapshotResult(ApplyCityCostOfLivingSnapshotStatus.Duplicate);

                    if (await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityCostOfLivingSnapshotResult(ApplyCityCostOfLivingSnapshotStatus.CityDeleted);

                    if (await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityCostOfLivingSnapshotResult(
                            ApplyCityCostOfLivingSnapshotStatus.CityArchived);

                    CityPopulationCostOfLivingState? state =
                        await cityPopulationCostOfLivingStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (state is not null && occurredAtUtc < state.LastEvaluatedAtUtc)
                        return new ApplyCityCostOfLivingSnapshotResult(ApplyCityCostOfLivingSnapshotStatus.Stale);

                    DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();

                    if (state is null)
                    {
                        state = CityPopulationCostOfLivingState.Create(
                            cityId: cityId,
                            wageMultiplier: request.WageMultiplier,
                            retailPriceMultiplier: request.RetailPriceMultiplier,
                            housingCostMultiplier: request.HousingCostMultiplier,
                            utilityCostMultiplier: request.UtilityCostMultiplier,
                            costOfLivingIndex: request.CostOfLivingIndex,
                            affordabilityIndex: request.AffordabilityIndex,
                            lastEvaluatedAtUtc: occurredAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationCostOfLivingStateRepository.AddAsync(
                            state: state,
                            cancellationToken: ct);
                    }
                    else
                        state.ApplySnapshot(
                            wageMultiplier: request.WageMultiplier,
                            retailPriceMultiplier: request.RetailPriceMultiplier,
                            housingCostMultiplier: request.HousingCostMultiplier,
                            utilityCostMultiplier: request.UtilityCostMultiplier,
                            costOfLivingIndex: request.CostOfLivingIndex,
                            affordabilityIndex: request.AffordabilityIndex,
                            lastEvaluatedAtUtc: occurredAtUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityCostOfLivingSnapshotResult(ApplyCityCostOfLivingSnapshotStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
