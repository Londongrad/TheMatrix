using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Architecture;

public sealed class ClassicCityInfrastructureBoundaryTests
{
    private const string ScenarioApplicationNamespace =
        "Matrix.SimulationSystems.Application.Scenarios.ClassicCity";

    private const string ScenarioInfrastructureNamespace =
        "Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity";

    [Fact]
    public void ScenarioContractImplementations_StayInsideClassicCityInfrastructure()
    {
        Type[] scenarioContracts = typeof(ICityEnvironmentalConditionRepository).Assembly
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
    public void ClassicCityNamedTypes_StayInsideClassicCityInfrastructure()
    {
        string[] misplacedTypes = typeof(DependencyInjection).Assembly
           .GetTypes()
           .Where(type =>
                type.Name.Contains(
                    value: "City",
                    comparisonType: StringComparison.Ordinal))
           .Where(type =>
                type.Namespace?.Contains(
                    value: ".Migrations",
                    comparisonType: StringComparison.Ordinal) != true)
           .Where(type => !IsClassicCityInfrastructureType(type))
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }

    private static bool IsClassicCityInfrastructureType(Type type)
    {
        return type.Namespace?.StartsWith(
                   value: ScenarioInfrastructureNamespace,
                   comparisonType: StringComparison.Ordinal) == true;
    }
}
