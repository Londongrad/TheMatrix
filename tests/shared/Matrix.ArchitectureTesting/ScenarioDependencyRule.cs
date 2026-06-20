using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Matrix.ArchitectureTesting
{
    public static class ScenarioDependencyRule
    {
        private static readonly string[] LayerNames =
        [
            "Domain",
            "Application",
            "Infrastructure",
            "Contracts",
            "Api"
        ];

        public static void AssertScenarioNeutral(
            Assembly assembly,
            string boundedContextNamespace,
            string scenarioName)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentException.ThrowIfNullOrWhiteSpace(boundedContextNamespace);
            ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

            string scenarioNamespaceSegment = $".Scenarios.{scenarioName}";
            string[] scenarioNamespaceRoots = LayerNames
               .Select(layer => $"{boundedContextNamespace}.{layer}{scenarioNamespaceSegment}")
               .Append($"Matrix.ScenarioContracts.{scenarioName}")
               .ToArray();

            TestResult result = Types
               .InAssembly(assembly)
               .That()
               .DoNotResideInNamespaceContaining(scenarioNamespaceSegment)
               .And()
               .DoNotResideInNamespaceContaining(".Migrations")
               .And()
               .DoNotHaveNameEndingWith("DbContext")
               .And()
               .DoNotHaveName("DependencyInjection")
               .ShouldNot()
               .HaveDependencyOnAny(scenarioNamespaceRoots)
               .GetResult();

            string dependencies = result.FailingTypes is null
                ? string.Empty
                : string.Join(
                    separator: Environment.NewLine,
                    values: result.FailingTypes
                       .Select(type => type.FullName)
                       .Where(typeName => typeName is not null)
                       .Order(StringComparer.Ordinal));

            Assert.True(result.IsSuccessful, dependencies);
        }
    }
}
