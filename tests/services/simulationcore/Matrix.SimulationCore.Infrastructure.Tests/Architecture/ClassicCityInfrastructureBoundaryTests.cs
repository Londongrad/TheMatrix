using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Architecture;

public sealed class ClassicCityInfrastructureBoundaryTests
{
    private const string ScenarioApplicationNamespace =
        "Matrix.SimulationCore.Application.Scenarios.ClassicCity";

    private const string ScenarioInfrastructureNamespace =
        "Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity";

    [Fact]
    public void ScenarioContractImplementations_StayInsideClassicCityInfrastructure()
    {
        Type[] scenarioContracts = typeof(IClassicCityOutboxWriter).Assembly
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
                type.Name.StartsWith(
                    value: "City",
                    comparisonType: StringComparison.Ordinal) ||
                type.Name.StartsWith(
                    value: "ClassicCity",
                    comparisonType: StringComparison.Ordinal))
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
