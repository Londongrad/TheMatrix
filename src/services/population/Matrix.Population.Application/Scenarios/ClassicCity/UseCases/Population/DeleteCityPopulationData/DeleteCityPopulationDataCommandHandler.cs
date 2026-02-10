using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.DeleteCityPopulationData
{
    public sealed class DeleteCityPopulationDataCommandHandler(
        IHouseholdWriteRepository householdWriteRepository,
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        ICityPopulationProgressionStateRepository cityPopulationProgressionStateRepository,
        ICityPopulationWeatherImpactStateRepository cityPopulationWeatherImpactStateRepository,
        ICityPopulationWeatherExposureStateRepository cityPopulationWeatherExposureStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteCityPopulationDataCommand, DeleteCityPopulationDataResult>
    {
        public Task<DeleteCityPopulationDataResult> Handle(
            DeleteCityPopulationDataCommand request,
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
                condition: request.DeletedAtUtc.Offset == TimeSpan.Zero,
                value: request.DeletedAtUtc,
                errorFactory: ApplicationErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(request.DeletedAtUtc));

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
                        return new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Duplicate);

                    CityPopulationDeletionState? deletionState =
                        await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (deletionState is not null && request.DeletedAtUtc < deletionState.DeletedAtUtc)
                        return new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Stale);

                    await householdWriteRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    await cityPopulationArchiveStateRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    await cityPopulationEnvironmentRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    await cityPopulationProgressionStateRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    await cityPopulationWeatherImpactStateRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    await cityPopulationWeatherExposureStateRepository.DeleteByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    if (deletionState is null)
                    {
                        var newDeletionState = CityPopulationDeletionState.Create(
                            cityId: cityId,
                            deletedAtUtc: request.DeletedAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationDeletionStateRepository.AddAsync(
                            state: newDeletionState,
                            cancellationToken: ct);
                    }
                    else
                        deletionState.MarkDeleted(
                            deletedAtUtc: request.DeletedAtUtc,
                            updatedAtUtc: updatedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
