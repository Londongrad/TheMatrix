using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;

namespace Matrix.SimulationCore.Application.Services.Simulation;

public sealed class SimulationScenarioAdvanceHandlerRegistry
{
    private readonly IReadOnlyDictionary<SimulationRuntimeKey, ISimulationScenarioAdvanceHandler> _handlers;

    public SimulationScenarioAdvanceHandlerRegistry(
        IEnumerable<ISimulationScenarioAdvanceHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        ISimulationScenarioAdvanceHandler[] registeredHandlers = handlers.ToArray();
        ISimulationScenarioAdvanceHandler? invalidHandler = registeredHandlers
           .FirstOrDefault(handler => handler.RuntimeKey.IsEmpty);

        if (invalidHandler is not null)
            throw new InvalidOperationException(
                $"Simulation advance handler '{invalidHandler.GetType().FullName}' has an empty runtime key.");

        IGrouping<SimulationRuntimeKey, ISimulationScenarioAdvanceHandler>? duplicate = registeredHandlers
           .GroupBy(handler => handler.RuntimeKey)
           .FirstOrDefault(group => group.Skip(1).Any());

        if (duplicate is not null)
            throw new InvalidOperationException(
                $"Multiple simulation advance handlers are registered for runtime '{duplicate.Key}'.");

        _handlers = registeredHandlers.ToDictionary(handler => handler.RuntimeKey);
    }

    public ISimulationScenarioAdvanceHandler Resolve(SimulationRuntimeKey runtimeKey)
    {
        if (runtimeKey.IsEmpty)
            throw new ArgumentException("A runtime key is required to resolve a simulation handler.", nameof(runtimeKey));

        return _handlers.TryGetValue(runtimeKey, out ISimulationScenarioAdvanceHandler? handler)
            ? handler
            : throw new InvalidOperationException(
                $"Simulation advance handler is not registered for runtime '{runtimeKey}'.");
    }
}
