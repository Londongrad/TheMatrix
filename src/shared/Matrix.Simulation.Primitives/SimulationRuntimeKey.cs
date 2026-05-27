namespace Matrix.Simulation.Primitives;

public readonly record struct SimulationRuntimeKey
{
    public SimulationRuntimeKey(
        SimulationScenarioKey scenarioKey,
        SimulationHostTypeKey hostTypeKey)
    {
        if (scenarioKey.IsEmpty)
            throw new ArgumentException("A runtime key requires a scenario key.", nameof(scenarioKey));

        if (hostTypeKey.IsEmpty)
            throw new ArgumentException("A runtime key requires a host type key.", nameof(hostTypeKey));

        ScenarioKey = scenarioKey;
        HostTypeKey = hostTypeKey;
    }

    public SimulationScenarioKey ScenarioKey { get; }
    public SimulationHostTypeKey HostTypeKey { get; }

    public bool IsEmpty => ScenarioKey.IsEmpty || HostTypeKey.IsEmpty;

    public override string ToString()
    {
        return IsEmpty
            ? string.Empty
            : $"{ScenarioKey}:{HostTypeKey}";
    }
}
