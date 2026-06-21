using Matrix.ArchitectureTesting;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Architecture;

public sealed class ClassicCityInfrastructureBoundaryTests
{
    private const string ScenarioApplicationNamespace = "Matrix.Population.Application.Scenarios.ClassicCity";
    private const string InfrastructureNamespace = "Matrix.Population.Infrastructure.";
    private const string ScenarioNamespaceSegment = ".Scenarios.ClassicCity";

    [Fact]
    public void ScenarioContractImplementations_StayInsideClassicCityInfrastructure()
    {
        Type[] scenarioContracts = typeof(ICityPopulationPersonReadRepository).Assembly
           .GetTypes()
           .Where(type =>
                type.IsInterface &&
                type.Namespace?.StartsWith(
                    value: ScenarioApplicationNamespace,
                    comparisonType: StringComparison.Ordinal) == true)
           .ToArray();

        string[] misplacedTypes = typeof(DependencyInjection).Assembly
           .GetTypes()
           .Where(type => !type.IsAbstract && !type.IsInterface)
           .Where(type => type.GetInterfaces().Intersect(scenarioContracts).Any())
           .Where(type => !IsClassicCityInfrastructureType(type))
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }

    [Fact]
    public void ScenarioNeutralInfrastructureTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(DependencyInjection).Assembly,
            boundedContextNamespace: "Matrix.Population",
            scenarioName: "ClassicCity");
    }

    private static bool IsClassicCityInfrastructureType(Type type)
    {
        return type.Namespace?.StartsWith(
                   InfrastructureNamespace,
                   StringComparison.Ordinal) == true &&
               type.Namespace.Contains(
                   ScenarioNamespaceSegment,
                   StringComparison.Ordinal);
    }
}
