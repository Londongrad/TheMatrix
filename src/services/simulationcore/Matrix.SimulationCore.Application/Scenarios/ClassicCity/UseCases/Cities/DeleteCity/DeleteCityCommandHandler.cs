using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity
{
    public sealed class DeleteCityCommandHandler(
        ISimulationInstanceRepository simulationInstanceRepository,
        ICityRepository cityRepository,
        ISimulationClockRepository clockRepository,
        ISimulationCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<DeleteCityCommand, DeleteCityResult>
    {
        public async Task<DeleteCityResult> Handle(
            DeleteCityCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return DeleteCityResult.NotFound;

            if (!city.IsArchived)
                return DeleteCityResult.NotAllowed;

            SimulationId simulationId = new(city.Id.Value);
            SimulationInstance? instance = await simulationInstanceRepository.GetByIdAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            if (instance is null)
                throw new InvalidOperationException(
                    $"Simulation instance '{simulationId}' is missing for classic city '{city.Id}'.");

            DateTimeOffset deletedAtUtc = timeProvider.GetUtcNow();

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    await outboxWriter.AddCityEventsAsync(
                        domainEvents:
                        [
                            new CityDeletedDomainEvent(
                                CityId: city.Id,
                                DeletedAtUtc: deletedAtUtc)
                        ],
                        cancellationToken: ct);
                    await clockRepository.DeleteBySimulationIdAsync(
                        simulationId: simulationId,
                        cancellationToken: ct);
                    simulationInstanceRepository.Delete(instance);
                    await cityRepository.DeleteAsync(
                        city: city,
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return DeleteCityResult.Deleted;
        }
    }
}
