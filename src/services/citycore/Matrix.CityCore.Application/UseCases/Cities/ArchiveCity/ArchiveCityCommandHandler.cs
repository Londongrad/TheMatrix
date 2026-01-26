using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.ArchiveCity
{
    public sealed class ArchiveCityCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockMutationExecutor simulationClockMutationExecutor,
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
                cityId: city.Id,
                mutate: clock => clock.Pause(),
                cancellationToken: cancellationToken,
                allowArchivedCity: true);

            city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null || city.IsArchived)
                return city is not null;

            city.Archive(DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}