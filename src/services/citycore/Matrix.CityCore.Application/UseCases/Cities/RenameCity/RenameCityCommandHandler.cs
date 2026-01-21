using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.RenameCity
{
    public sealed class RenameCityCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RenameCityCommand, bool>
    {
        public async Task<bool> Handle(
            RenameCityCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            city.Rename(new CityName(request.Name));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
