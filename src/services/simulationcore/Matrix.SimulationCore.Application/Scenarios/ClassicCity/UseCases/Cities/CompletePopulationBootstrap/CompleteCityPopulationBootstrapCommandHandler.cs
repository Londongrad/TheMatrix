using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap
{
    public sealed class CompleteCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        ISimulationInstanceRepository simulationInstanceRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<CompleteCityPopulationBootstrapCommand, bool>
    {
        public async Task<bool> Handle(
            CompleteCityPopulationBootstrapCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            bool updated = city.TryCompletePopulationBootstrap(
                operationId: request.OperationId,
                completedAtUtc: timeProvider.GetUtcNow());

            if (updated)
            {
                if (city.IsActive)
                {
                    SimulationInstance instance = await simulationInstanceRepository.GetRequiredByHostAsync(
                        runtimeKey: ClassicCityRuntime.Key,
                        hostId: new SimulationHostId(city.Id.Value),
                        cancellationToken: cancellationToken);
                    instance.Activate();
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
