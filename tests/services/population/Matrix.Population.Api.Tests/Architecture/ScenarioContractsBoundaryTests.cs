using Matrix.ArchitectureTesting;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Population.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void ScenarioNeutralContracts_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(CityResidentDetailsDto).Assembly,
            boundedContextNamespace: "Matrix.Population",
            scenarioName: "ClassicCity");
    }
}
