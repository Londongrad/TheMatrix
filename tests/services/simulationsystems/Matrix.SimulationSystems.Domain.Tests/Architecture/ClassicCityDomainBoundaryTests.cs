using Matrix.ArchitectureTesting;
using Matrix.SimulationSystems.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Architecture;

public sealed class ClassicCityDomainBoundaryTests
{
    [Fact]
    public void ScenarioNeutralDomainTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(SimulationHostId).Assembly,
            boundedContextNamespace: "Matrix.SimulationSystems",
            scenarioName: "ClassicCity");
    }
}
