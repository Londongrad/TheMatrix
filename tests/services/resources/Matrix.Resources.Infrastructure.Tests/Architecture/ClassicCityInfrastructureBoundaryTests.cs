using Matrix.ArchitectureTesting;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Architecture;

public sealed class ClassicCityInfrastructureBoundaryTests
{
    private const string ScenarioApplicationNamespace =
        "Matrix.Resources.Application.Scenarios.ClassicCity";

    private const string ScenarioInfrastructureNamespace =
        "Matrix.Resources.Infrastructure.Scenarios.ClassicCity";

    [Fact]
    public void ScenarioContractImplementations_StayInsideClassicCityInfrastructure()
    {
        Type[] scenarioContracts = typeof(ICityStockpileRepository).Assembly
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
            boundedContextNamespace: "Matrix.Resources",
            scenarioName: "ClassicCity");
    }

    private static bool IsClassicCityInfrastructureType(Type type)
    {
        return type.Namespace?.StartsWith(
                   value: ScenarioInfrastructureNamespace,
                   comparisonType: StringComparison.Ordinal) == true;
    }
}
