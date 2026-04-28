using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<FailCityPopulationBootstrapCommand, bool>
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
                failedAtUtc: timeProvider.GetUtcNow());

            if (updated)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
