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
           .Where(type =>
                type.Namespace?.StartsWith(
                    value: InfrastructureNamespace,
                    comparisonType: StringComparison.Ordinal) != true ||
                type.Namespace.Contains(
                    value: ScenarioNamespaceSegment,
                    comparisonType: StringComparison.Ordinal) == false)
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }
}
