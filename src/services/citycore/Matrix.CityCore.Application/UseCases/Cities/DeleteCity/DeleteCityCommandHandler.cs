using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.DeleteCity
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
                    await clockRepository.DeleteByCityIdAsync(
                        cityId: city.Id,
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
