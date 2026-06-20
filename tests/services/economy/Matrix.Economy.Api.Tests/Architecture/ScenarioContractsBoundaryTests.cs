using Matrix.ArchitectureTesting;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views;
using Xunit;

namespace Matrix.Economy.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void ScenarioNeutralContracts_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(CityEconomyBootstrapResultView).Assembly,
            boundedContextNamespace: "Matrix.Economy",
            scenarioName: "ClassicCity");
    }
}
