using Matrix.ArchitectureTesting;
using Xunit;

namespace Matrix.Economy.Application.Tests.Architecture;

public sealed class ClassicCityApplicationBoundaryTests
{
    [Fact]
    public void ScenarioNeutralApplicationTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(DependencyInjection).Assembly,
            boundedContextNamespace: "Matrix.Economy",
            scenarioName: "ClassicCity");
    }
}
