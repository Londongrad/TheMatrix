using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.ArchiveCity
{
    public sealed class ArchiveCityCommandHandler(
        ICityRepository cityRepository,
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

            city.Archive(DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
