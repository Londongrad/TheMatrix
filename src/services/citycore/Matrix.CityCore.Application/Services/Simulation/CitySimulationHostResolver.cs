using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed class CitySimulationHostResolver(ICityRepository cityRepository) : ISimulationHostResolver
    {
        public async Task<SimulationHostDescriptor?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(simulationId.Value);

            City? city = await cityRepository.GetByIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return city is null
                ? null
                : new SimulationHostDescriptor(
                    SimulationId: simulationId,
                    HostId: new SimulationHostId(city.Id.Value),
                    HostKind: SimulationHostKind.City,
                    SimulationKind: city.SimulationKind,
                    IsActive: city.IsActive,
                    IsArchived: city.IsArchived);
        }
    }
}
