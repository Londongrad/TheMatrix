using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.DeleteCity
{
    public sealed class DeleteCityCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockRepository clockRepository,
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

            return DeleteCityResult.Deleted;
        }
    }
}
