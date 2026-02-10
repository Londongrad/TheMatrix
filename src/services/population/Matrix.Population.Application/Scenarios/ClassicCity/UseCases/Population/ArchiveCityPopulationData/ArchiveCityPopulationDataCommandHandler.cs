using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData
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
            GuardHelper.AgainstEmptyGuid(
                id: request.CityId,
                errorFactory: ApplicationErrorsFactory.EmptyId,
                propertyName: nameof(request.CityId));
            GuardHelper.AgainstEmptyGuid(
                id: request.IntegrationMessageId,
                errorFactory: ApplicationErrorsFactory.EmptyId,
                propertyName: nameof(request.IntegrationMessageId));

            string consumerName = GuardHelper.AgainstNullOrWhiteSpace(
                value: request.ConsumerName,
                errorFactory: ApplicationErrorsFactory.Required,
                propertyName: nameof(request.ConsumerName));
            GuardHelper.Ensure(
                condition: request.ArchivedAtUtc.Offset == TimeSpan.Zero,
                value: request.ArchivedAtUtc,
                errorFactory: ApplicationErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(request.ArchivedAtUtc));

            var cityId = CityId.From(request.CityId);

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: consumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.Duplicate);

                    CityPopulationDeletionState? deletionState =
                        await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (deletionState is not null)
                        return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.CityDeleted);

                    CityPopulationArchiveState? archiveState =
                        await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (archiveState is not null && request.ArchivedAtUtc < archiveState.ArchivedAtUtc)
                        return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.Stale);

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (archiveState is null)
                    {
                        var newArchiveState = CityPopulationArchiveState.Create(
                            cityId: cityId,
                            archivedAtUtc: request.ArchivedAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationArchiveStateRepository.AddAsync(
                            state: newArchiveState,
                            cancellationToken: ct);
                    }
                    else
                        archiveState.MarkArchived(
                            archivedAtUtc: request.ArchivedAtUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ArchiveCityPopulationDataResult(ArchiveCityPopulationDataStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
