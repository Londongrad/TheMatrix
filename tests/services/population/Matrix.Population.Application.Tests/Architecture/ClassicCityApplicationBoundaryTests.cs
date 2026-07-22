using Matrix.ArchitectureTesting;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using NetArchTest.Rules;
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

        [Fact]
        public void PopulationTick_DoesNotDependOnEducationIntegrationDetails()
        {
            TestResult result = Types
               .InAssembly(typeof(AdvanceCityPopulationCommand).Assembly)
               .That()
               .ResideInNamespace(
                    "Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation")
               .ShouldNot()
               .HaveDependencyOn("Matrix.Population.Application.Integration.Education")
               .GetResult();

            Assert.True(
                condition: result.IsSuccessful,
                userMessage: string.Join(
                    separator: Environment.NewLine,
                    values: result.FailingTypeNames ?? []));
        }
    }
}
