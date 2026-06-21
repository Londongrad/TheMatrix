using Matrix.ArchitectureTesting;
using Matrix.Resources.Domain.Simulation;
using Xunit;

namespace Matrix.Resources.Domain.Tests.Architecture;

public sealed class ClassicCityDomainBoundaryTests
{
    [Fact]
    public void ScenarioNeutralDomainTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(SimulationHostId).Assembly,
            boundedContextNamespace: "Matrix.Resources",
            scenarioName: "ClassicCity");
    }
}
