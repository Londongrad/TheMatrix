using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.ArchiveCityPopulationData
{
    public sealed class ArchiveCityPopulationDataCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ArchiveCityPopulationDataCommand, ArchiveCityPopulationDataResult>
    {
        public Task<ArchiveCityPopulationDataResult> Handle(
            ArchiveCityPopulationDataCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CityId == Guid.Empty)
                throw new ArgumentException("CityId cannot be empty.", nameof(request.CityId));

            if (request.IntegrationMessageId == Guid.Empty)
                throw new ArgumentException("IntegrationMessageId cannot be empty.", nameof(request.IntegrationMessageId));

            if (string.IsNullOrWhiteSpace(request.ConsumerName))
                throw new ArgumentException("ConsumerName is required.", nameof(request.ConsumerName));

            if (request.ArchivedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("ArchivedAtUtc must be UTC.", nameof(request.ArchivedAtUtc));

            CityId cityId = CityId.From(request.CityId);

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: request.ConsumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.Duplicate);

                    CityPopulationDeletionState? deletionState = await cityPopulationDeletionStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (deletionState is not null)
                        return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.CityDeleted);

                    CityPopulationArchiveState? archiveState = await cityPopulationArchiveStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (archiveState is not null && request.ArchivedAtUtc < archiveState.ArchivedAtUtc)
                        return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.Stale);

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (archiveState is null)
                    {
                        CityPopulationArchiveState newArchiveState = CityPopulationArchiveState.Create(
                            cityId: cityId,
                            archivedAtUtc: request.ArchivedAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationArchiveStateRepository.AddAsync(
                            state: newArchiveState,
                            cancellationToken: ct);
                    }
                    else
                    {
                        archiveState.MarkArchived(
                            archivedAtUtc: request.ArchivedAtUtc,
                            updatedAtUtc: updatedAtUtc);
                    }

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
