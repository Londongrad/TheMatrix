using Matrix.ArchitectureTesting;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Architecture;

public sealed class ClassicCityDomainBoundaryTests
{
    [Fact]
    public void ScenarioNeutralDomainTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(CityBudgetId).Assembly,
            boundedContextNamespace: "Matrix.Economy",
            scenarioName: "ClassicCity");
    }
}
