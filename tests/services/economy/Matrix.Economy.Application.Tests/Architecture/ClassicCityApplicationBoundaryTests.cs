using Xunit;

namespace Matrix.Economy.Application.Tests.Architecture;

public sealed class ClassicCityApplicationBoundaryTests
{
    private const string ScenarioApplicationNamespace =
        "Matrix.Economy.Application.Scenarios.ClassicCity";

    [Fact]
    public void ClassicCityNamedTypes_StayInsideClassicCityApplication()
    {
        string[] misplacedTypes = typeof(DependencyInjection).Assembly
           .GetTypes()
           .Where(IsClassicCityNamedType)
           .Where(type => !IsClassicCityApplicationType(type))
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }

    private static bool IsClassicCityNamedType(Type type)
    {
        return type.Name.Contains(
                   value: "City",
                   comparisonType: StringComparison.Ordinal) ||
               type.Name.Contains(
                   value: "ClassicCity",
                   comparisonType: StringComparison.Ordinal);
    }

    private static bool IsClassicCityApplicationType(Type type)
    {
        return type.Namespace?.StartsWith(
                   value: ScenarioApplicationNamespace,
                   comparisonType: StringComparison.Ordinal) == true;
    }
}
