using Matrix.ArchitectureTesting;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Xunit;

namespace Matrix.Resources.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void ScenarioNeutralContracts_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(CityStockpilesView).Assembly,
            boundedContextNamespace: "Matrix.Resources",
            scenarioName: "ClassicCity");
    }
}
