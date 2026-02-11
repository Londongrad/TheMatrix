using Matrix.CityCore.Domain.Events.Simulation;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationScenarioAdvanceHandler
    {
        SimulationHostKind HostKind { get; }

        Task HandleAdvancedAsync(
            SimulationHost host,
            SimulationTimeAdvancedDomainEvent advancedEvent,
            CancellationToken cancellationToken);
    }
}
