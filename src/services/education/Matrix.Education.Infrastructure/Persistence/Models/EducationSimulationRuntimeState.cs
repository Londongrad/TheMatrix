using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Infrastructure.Persistence.Models;

public sealed class EducationSimulationRuntimeState
{
    private EducationSimulationRuntimeState() { }

    public EducationSimulationRuntimeState(SimulationHostId hostId, SimulationRuntimeKey runtimeKey)
    {
        if (runtimeKey.IsEmpty)
            throw new ArgumentException("An education runtime is required.", nameof(runtimeKey));
        SimulationHostId = hostId;
        ScenarioKey = runtimeKey.ScenarioKey.Value;
        HostTypeKey = runtimeKey.HostTypeKey.Value;
    }

    public SimulationHostId SimulationHostId { get; private set; }
    public string ScenarioKey { get; private set; } = string.Empty;
    public string HostTypeKey { get; private set; } = string.Empty;

    public SimulationRuntimeKey ToRuntimeKey() => new(new SimulationScenarioKey(ScenarioKey), new SimulationHostTypeKey(HostTypeKey));
}
