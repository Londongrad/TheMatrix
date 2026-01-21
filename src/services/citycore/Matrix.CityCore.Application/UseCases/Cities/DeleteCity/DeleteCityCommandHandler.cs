using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.DeleteCity
{
    public sealed class DeleteCityCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockRepository clockRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<DeleteCityCommand, bool>
    {
        public async Task<bool> Handle(
            DeleteCityCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            if (!city.IsArchived)
                return false;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    await clockRepository.DeleteByCityIdAsync(
                        cityId: city.Id,
                        cancellationToken: ct);
                    await cityRepository.DeleteAsync(
                        city: city,
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return true;
        }
    }
}
