using Matrix.ArchitectureTesting;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Xunit;

namespace Matrix.Resources.Application.Tests.Architecture;

public sealed class ClassicCityApplicationBoundaryTests
{
    [Fact]
    public void ScenarioNeutralApplicationTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(SyncCitySystemsDemandCommand).Assembly,
            boundedContextNamespace: "Matrix.Resources",
            scenarioName: "ClassicCity");
    }
}
