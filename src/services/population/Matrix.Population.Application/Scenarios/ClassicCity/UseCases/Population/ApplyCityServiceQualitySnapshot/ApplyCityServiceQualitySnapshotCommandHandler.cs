using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot
{
    public sealed class ApplyCityServiceQualitySnapshotCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationServiceQualityStateRepository cityPopulationServiceQualityStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityServiceQualitySnapshotCommand, ApplyCityServiceQualitySnapshotResult>
    {
        public Task<ApplyCityServiceQualitySnapshotResult> Handle(
            ApplyCityServiceQualitySnapshotCommand request,
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
                        return new ApplyCityServiceQualitySnapshotResult(
                            ApplyCityServiceQualitySnapshotStatus.Duplicate);

                    if (await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityServiceQualitySnapshotResult(
                            ApplyCityServiceQualitySnapshotStatus.CityDeleted);

                    if (await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityServiceQualitySnapshotResult(
                            ApplyCityServiceQualitySnapshotStatus.CityArchived);

                    CityPopulationServiceQualityState? state =
                        await cityPopulationServiceQualityStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (state is not null && occurredAtUtc < state.LastEvaluatedAtUtc)
                        return new ApplyCityServiceQualitySnapshotResult(ApplyCityServiceQualitySnapshotStatus.Stale);

                    DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();

                    if (state is null)
                    {
                        state = CityPopulationServiceQualityState.Create(
                            cityId: cityId,
                            healthcareQualityIndex: request.HealthcareQualityIndex,
                            housingSupportIndex: request.HousingSupportIndex,
                            lastEvaluatedAtUtc: occurredAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationServiceQualityStateRepository.AddAsync(
                            state: state,
                            cancellationToken: ct);
                    }
                    else
                        state.ApplySnapshot(
                            healthcareQualityIndex: request.HealthcareQualityIndex,
                            housingSupportIndex: request.HousingSupportIndex,
                            lastEvaluatedAtUtc: occurredAtUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityServiceQualitySnapshotResult(ApplyCityServiceQualitySnapshotStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
