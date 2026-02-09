using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity
{
    public sealed class DeleteCityCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockRepository clockRepository,
        ICityCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : IRequestHandler<DeleteCityCommand, DeleteCityResult>
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

            DateTimeOffset deletedAtUtc = DateTimeOffset.UtcNow;

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
                        simulationId: new SimulationId(city.Id.Value),
                        cancellationToken: ct);
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
