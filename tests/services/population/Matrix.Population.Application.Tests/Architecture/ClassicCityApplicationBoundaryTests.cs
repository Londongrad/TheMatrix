using Matrix.ArchitectureTesting;
using Matrix.Population.Application.Abstractions;
using Xunit;

namespace Matrix.Population.Application.Tests.Architecture
{
    public sealed class ClassicCityApplicationBoundaryTests
    {
        [Fact]
        public void ScenarioNeutralApplicationTypes_DoNotDependOnClassicCity()
        {
            ScenarioDependencyRule.AssertScenarioNeutral(
                assembly: typeof(IPersonLifecycleExtension).Assembly,
                boundedContextNamespace: "Matrix.Population",
                scenarioName: "ClassicCity");
        }
    }
}
