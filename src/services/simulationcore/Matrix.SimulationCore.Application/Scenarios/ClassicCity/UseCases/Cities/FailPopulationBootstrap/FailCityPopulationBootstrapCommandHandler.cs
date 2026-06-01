using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        ISimulationInstanceRepository simulationInstanceRepository,
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
            {
                SimulationInstance instance = await simulationInstanceRepository.GetRequiredByHostAsync(
                    runtimeKey: ClassicCityRuntime.Key,
                    hostId: new SimulationHostId(city.Id.Value),
                    cancellationToken: cancellationToken);
                instance.FailProvisioning();

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
