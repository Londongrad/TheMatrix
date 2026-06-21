using Matrix.ArchitectureTesting;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Xunit;

namespace Matrix.SimulationCore.Api.Tests.Architecture
{
    public sealed class ScenarioContractsBoundaryTests
    {
        [Fact]
        public void ScenarioNeutralContracts_DoNotDependOnClassicCity()
        {
            ScenarioDependencyRule.AssertScenarioNeutral(
                assembly: typeof(ClassicCityCreatedV1).Assembly,
                boundedContextNamespace: "Matrix.SimulationCore",
                scenarioName: "ClassicCity");
        }
    }
}
