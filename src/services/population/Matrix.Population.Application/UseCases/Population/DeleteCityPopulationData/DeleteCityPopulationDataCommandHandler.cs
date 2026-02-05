using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.DeleteCityPopulationData
{
    public sealed class DeleteCityPopulationDataCommandHandler(
        IHouseholdWriteRepository householdWriteRepository,
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
            if (request.CityId == Guid.Empty)
                throw new ArgumentException("CityId cannot be empty.", nameof(request.CityId));

            if (request.IntegrationMessageId == Guid.Empty)
                throw new ArgumentException("IntegrationMessageId cannot be empty.", nameof(request.IntegrationMessageId));

            if (string.IsNullOrWhiteSpace(request.ConsumerName))
                throw new ArgumentException("ConsumerName is required.", nameof(request.ConsumerName));

            if (request.DeletedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("DeletedAtUtc must be UTC.", nameof(request.DeletedAtUtc));

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
                        return new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Duplicate);

                    CityPopulationDeletionState? deletionState = await cityPopulationDeletionStateRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (deletionState is not null && request.DeletedAtUtc < deletionState.DeletedAtUtc)
                        return new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Stale);

                    await householdWriteRepository.DeleteByCityAsync(
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
                        CityPopulationDeletionState newDeletionState = CityPopulationDeletionState.Create(
                            cityId: cityId,
                            deletedAtUtc: request.DeletedAtUtc,
                            updatedAtUtc: updatedAtUtc);

                        await cityPopulationDeletionStateRepository.AddAsync(
                            state: newDeletionState,
                            cancellationToken: ct);
                    }
                    else
                    {
                        deletionState.MarkDeleted(
                            deletedAtUtc: request.DeletedAtUtc,
                            updatedAtUtc: updatedAtUtc);
                    }

                    await unitOfWork.SaveChangesAsync(ct);

                    return new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
