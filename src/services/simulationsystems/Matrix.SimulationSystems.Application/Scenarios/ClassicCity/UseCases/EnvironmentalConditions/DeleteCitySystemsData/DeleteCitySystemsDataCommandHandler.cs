using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData
{
    public sealed class DeleteCitySystemsDataCommandHandler(
        ICityEnvironmentalConditionRepository conditionRepository,
        ICitySystemsDeletionStateRepository deletionStateRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<DeleteCitySystemsDataCommand, DeleteCitySystemsDataResult>
    {
        public Task<DeleteCitySystemsDataResult> Handle(
            DeleteCitySystemsDataCommand request,
            CancellationToken cancellationToken)
        {
            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset? deletedAtUtc = await deletionStateRepository.GetDeletedAtUtcAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);

                    if (deletedAtUtc == request.DeletedAtUtc)
                        return new DeleteCitySystemsDataResult(DeleteCitySystemsDataStatus.Duplicate);

                    if (deletedAtUtc > request.DeletedAtUtc)
                        return new DeleteCitySystemsDataResult(DeleteCitySystemsDataStatus.Stale);

                    await conditionRepository.DeleteBySimulationHostIdAsync(
                        simulationHostId: new SimulationHostId(request.CityId),
                        cancellationToken: ct);
                    await deletionStateRepository.RecordAsync(
                        cityId: request.CityId,
                        deletedAtUtc: request.DeletedAtUtc,
                        updatedAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    return new DeleteCitySystemsDataResult(DeleteCitySystemsDataStatus.Applied);
                },
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }
    }
}
