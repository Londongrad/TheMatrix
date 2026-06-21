using Matrix.ArchitectureTesting;
using Matrix.Population.Domain.Entities;
using Xunit;

namespace Matrix.Population.Domain.Tests.Architecture
{
    public sealed class ClassicCityDomainBoundaryTests
    {
        [Fact]
        public void ScenarioNeutralDomainTypes_DoNotDependOnClassicCity()
        {
            ScenarioDependencyRule.AssertScenarioNeutral(
                assembly: typeof(Person).Assembly,
                boundedContextNamespace: "Matrix.Population",
                scenarioName: "ClassicCity");
        }
    }
}
