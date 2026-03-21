using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Simulation.Abstractions
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
