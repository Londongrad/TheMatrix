using Matrix.ArchitectureTesting;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Architecture;

public sealed class ScenarioIsolationTests
{
    [Fact]
    public void ScenarioNeutralDomainTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(SimulationClock).Assembly,
            boundedContextNamespace: "Matrix.SimulationCore",
            scenarioName: "ClassicCity");
    }
}
