using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot
{
    public sealed class ApplyCityLivingConditionsSnapshotCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationLivingConditionsStateRepository cityPopulationLivingConditionsStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityLivingConditionsSnapshotCommand, ApplyCityLivingConditionsSnapshotResult>
    {
        public Task<ApplyCityLivingConditionsSnapshotResult> Handle(
            ApplyCityLivingConditionsSnapshotCommand request,
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
                        return new ApplyCityLivingConditionsSnapshotResult(
                            ApplyCityLivingConditionsSnapshotStatus.Duplicate);

                    if (await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityLivingConditionsSnapshotResult(
                            ApplyCityLivingConditionsSnapshotStatus.CityDeleted);

                    if (await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityLivingConditionsSnapshotResult(
                            ApplyCityLivingConditionsSnapshotStatus.CityArchived);

                    CityPopulationLivingConditionsState? state =
                        await cityPopulationLivingConditionsStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (state is not null &&
                        (request.EffectiveTickId < state.EffectiveTickId ||
                         (request.EffectiveTickId == state.EffectiveTickId &&
                          effectiveAtUtc <= state.EffectiveAtUtc)))
                        return new ApplyCityLivingConditionsSnapshotResult(
                            ApplyCityLivingConditionsSnapshotStatus.Stale);

                    DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();

                    if (state is null)
                    {
                        state = CityPopulationLivingConditionsState.Create(
                            cityId: cityId,
                            floodingIndex: request.FloodingIndex,
                            roadAccessibilityIndex: request.RoadAccessibilityIndex,
                            powerCoverageIndex: request.PowerCoverageIndex,
                            utilityContinuityIndex: request.UtilityContinuityIndex,
                            heatingCoverageIndex: request.HeatingCoverageIndex,
                            waterCoverageIndex: request.WaterCoverageIndex,
                            sanitationCoverageIndex: request.SanitationCoverageIndex,
                            effectiveTickId: request.EffectiveTickId,
                            effectiveAtUtc: effectiveAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationLivingConditionsStateRepository.AddAsync(
                            state: state,
                            cancellationToken: ct);
                    }
                    else
                        state.ApplySnapshot(
                            floodingIndex: request.FloodingIndex,
                            roadAccessibilityIndex: request.RoadAccessibilityIndex,
                            powerCoverageIndex: request.PowerCoverageIndex,
                            utilityContinuityIndex: request.UtilityContinuityIndex,
                            heatingCoverageIndex: request.HeatingCoverageIndex,
                            waterCoverageIndex: request.WaterCoverageIndex,
                            sanitationCoverageIndex: request.SanitationCoverageIndex,
                            effectiveTickId: request.EffectiveTickId,
                            effectiveAtUtc: effectiveAtUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityLivingConditionsSnapshotResult(ApplyCityLivingConditionsSnapshotStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
