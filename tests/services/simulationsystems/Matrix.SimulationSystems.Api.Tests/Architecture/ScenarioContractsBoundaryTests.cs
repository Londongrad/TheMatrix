using Matrix.ArchitectureTesting;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity;
using Xunit;

namespace Matrix.SimulationSystems.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void ScenarioNeutralContracts_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(ClassicCitySimulationSystemsApiRoutes).Assembly,
            boundedContextNamespace: "Matrix.SimulationSystems",
            scenarioName: "ClassicCity");
    }
}
