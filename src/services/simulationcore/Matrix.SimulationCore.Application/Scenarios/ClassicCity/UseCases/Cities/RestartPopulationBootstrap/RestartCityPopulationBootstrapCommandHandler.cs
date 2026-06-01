using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed class RestartCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        ISimulationInstanceRepository simulationInstanceRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
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
                restartedAtUtc: timeProvider.GetUtcNow(),
                plannedPeopleCountOverride: request.PlannedPeopleCountOverride,
                populationOperationId: out Guid populationOperationId,
                economyOperationId: out Guid economyOperationId);

            if (!restarted)
                return RestartCityPopulationBootstrapResult.NotAllowed();

            SimulationInstance instance = await simulationInstanceRepository.GetRequiredByHostAsync(
                runtimeKey: ClassicCityRuntime.Key,
                hostId: new SimulationHostId(city.Id.Value),
                cancellationToken: cancellationToken);
            instance.RestartProvisioning();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return RestartCityPopulationBootstrapResult.Restarted(
                populationOperationId: populationOperationId,
                economyOperationId: economyOperationId);
        }
    }
}
