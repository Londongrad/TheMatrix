using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources
{
    public sealed class DeleteCityResourcesCommandHandler(
        ICityStockpileRepository stockpileRepository,
        ICityResourceDeletionStateRepository deletionStateRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<DeleteCityResourcesCommand, DeleteCityResourcesResult>
    {
        public Task<DeleteCityResourcesResult> Handle(
            DeleteCityResourcesCommand request,
            CancellationToken cancellationToken)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset? deletedAtUtc = await deletionStateRepository.GetDeletedAtUtcAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);

                    if (deletedAtUtc == request.DeletedAtUtc)
                        return new DeleteCityResourcesResult(DeleteCityResourcesStatus.Duplicate);

                    if (deletedAtUtc > request.DeletedAtUtc)
                        return new DeleteCityResourcesResult(DeleteCityResourcesStatus.Stale);

                    await stockpileRepository.DeleteBySimulationHostIdAsync(
                        simulationHostId: new SimulationHostId(request.CityId),
                        cancellationToken: ct);
                    await deletionStateRepository.RecordAsync(
                        cityId: request.CityId,
                        deletedAtUtc: request.DeletedAtUtc,
                        updatedAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    return new DeleteCityResourcesResult(DeleteCityResourcesStatus.Applied);
                },
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }
    }
}
