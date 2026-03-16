using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed class RestartCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RestartCityPopulationBootstrapCommand, RestartCityPopulationBootstrapResult>
    {
        public async Task<RestartCityPopulationBootstrapResult> Handle(
            RestartCityPopulationBootstrapCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return RestartCityPopulationBootstrapResult.NotFound();

            if (city.IsArchived || city.Status != CityStatus.ProvisioningFailed)
                return RestartCityPopulationBootstrapResult.NotAllowed();

            bool restarted = city.TryRestartPopulationBootstrap(
                restartedAtUtc: DateTimeOffset.UtcNow,
                populationOperationId: out Guid populationOperationId,
                economyOperationId: out Guid economyOperationId);

            if (!restarted)
                return RestartCityPopulationBootstrapResult.NotAllowed();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return RestartCityPopulationBootstrapResult.Restarted(
                populationOperationId: populationOperationId,
                economyOperationId: economyOperationId,
                simulationKind: city.SimulationKind.ToString());
        }
    }
}
