using System.Data;
using Matrix.Economy.Application.Abstractions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData
{
    public sealed class DeleteCityEconomyDataCommandHandler(
        ICityEconomyDeletionRepository deletionRepository,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<DeleteCityEconomyDataCommand, DeleteCityEconomyDataResult>
    {
        public Task<DeleteCityEconomyDataResult> Handle(
            DeleteCityEconomyDataCommand request,
            CancellationToken cancellationToken)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);

                    if (deletedAtUtc == request.DeletedAtUtc)
                        return new DeleteCityEconomyDataResult(DeleteCityEconomyDataStatus.Duplicate);

                    if (deletedAtUtc > request.DeletedAtUtc)
                        return new DeleteCityEconomyDataResult(DeleteCityEconomyDataStatus.Stale);

                    await deletionRepository.DeleteCityDataAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);
                    await deletionRepository.RecordAsync(
                        cityId: request.CityId,
                        deletedAtUtc: request.DeletedAtUtc,
                        updatedAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    return new DeleteCityEconomyDataResult(DeleteCityEconomyDataStatus.Applied);
                },
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }
    }
}
