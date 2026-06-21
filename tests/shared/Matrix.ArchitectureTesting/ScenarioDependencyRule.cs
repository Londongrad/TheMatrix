using System.Reflection;
using Mono.Cecil;
using NetArchTest.Rules;

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
               .MeetCustomRule(new ScenarioNeutralTypeRule(scenarioNamespaceSegment))
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

            if (!result.IsSuccessful)
                throw new InvalidOperationException(
                    $"Scenario-neutral types depend on scenario code:{Environment.NewLine}{dependencies}");
        }

        private sealed class ScenarioNeutralTypeRule(string scenarioNamespaceSegment) : ICustomRule
        {
            public bool MeetsRule(TypeDefinition typeDefinition)
            {
                string effectiveNamespace = GetEffectiveNamespace(typeDefinition);

                return !effectiveNamespace.Contains(
                           scenarioNamespaceSegment,
                           StringComparison.Ordinal) &&
                       !effectiveNamespace.Contains(
                           ".Migrations",
                           StringComparison.Ordinal) &&
                       !typeDefinition.Name.EndsWith(
                           "DbContext",
                           StringComparison.Ordinal) &&
                       !string.Equals(
                           typeDefinition.Name,
                           "DependencyInjection",
                           StringComparison.Ordinal);
            }

            private static string GetEffectiveNamespace(TypeDefinition typeDefinition)
            {
                TypeDefinition current = typeDefinition;

                while (string.IsNullOrEmpty(current.Namespace) && current.DeclaringType is not null)
                    current = current.DeclaringType;

                return current.Namespace ?? string.Empty;
            }
        }
    }
}
