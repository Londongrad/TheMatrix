using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.ArchiveCity
{
    public sealed class ArchiveCityCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockMutationExecutor simulationClockMutationExecutor,
        ICityCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : IRequestHandler<ArchiveCityCommand, bool>
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
                allowArchivedCity: true);

            city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null || city.IsArchived)
                return city is not null;

            city.Archive(DateTimeOffset.UtcNow);
            await outboxWriter.AddCityEventsAsync(
                domainEvents: city.DomainEvents,
                cancellationToken: cancellationToken);
            city.ClearDomainEvents();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
