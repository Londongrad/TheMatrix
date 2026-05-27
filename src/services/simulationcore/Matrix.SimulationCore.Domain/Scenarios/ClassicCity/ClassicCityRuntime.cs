using Matrix.Simulation.Primitives;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity;

public static class ClassicCityRuntime
{
    public static readonly SimulationScenarioKey ScenarioKey = new("classic-city");
    public static readonly SimulationHostTypeKey HostTypeKey = new("city");
    public static readonly SimulationRuntimeKey Key = new(ScenarioKey, HostTypeKey);
}
