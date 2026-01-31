using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<FailCityPopulationBootstrapCommand, bool>
    {
        public async Task<bool> Handle(
            FailCityPopulationBootstrapCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            bool updated = city.TryFailPopulationBootstrap(
                operationId: request.OperationId,
                failureCode: request.FailureCode,
                failedAtUtc: DateTimeOffset.UtcNow);

            if (updated)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
