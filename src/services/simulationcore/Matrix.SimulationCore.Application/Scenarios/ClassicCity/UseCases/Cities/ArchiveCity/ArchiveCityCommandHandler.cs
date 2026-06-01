using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Events;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity
{
    public sealed class ArchiveCityCommandHandler(
        ISimulationInstanceRepository simulationInstanceRepository,
        ICityRepository cityRepository,
        ISimulationClockMutationExecutor simulationClockMutationExecutor,
        ISimulationCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<ArchiveCityCommand, bool>
    {
        public async Task<bool> Handle(
            ArchiveCityCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            if (city.IsArchived)
                return true;

            _ = await simulationClockMutationExecutor.ExecuteAsync(
                simulationId: new SimulationId(city.Id.Value),
                mutate: clock => clock.Pause(),
                cancellationToken: cancellationToken,
                allowArchivedHost: true);

            city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null || city.IsArchived)
                return city is not null;

            SimulationId simulationId = new(city.Id.Value);
            SimulationInstance? instance = await simulationInstanceRepository.GetByIdAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            if (instance is null)
                throw new InvalidOperationException(
                    $"Simulation instance '{simulationId}' is missing for classic city '{city.Id}'.");

            DateTimeOffset archivedAtUtc = timeProvider.GetUtcNow();
            instance.Archive(archivedAtUtc);
            city.Archive(archivedAtUtc);
            await DomainEventDispatchHelper.PublishAndClearAsync(
                source: instance,
                publish: outboxWriter.AddSimulationEventsAsync,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
