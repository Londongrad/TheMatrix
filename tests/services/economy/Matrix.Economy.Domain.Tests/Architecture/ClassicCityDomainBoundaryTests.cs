using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Architecture;

public sealed class ClassicCityDomainBoundaryTests
{
    private const string ScenarioDomainNamespace =
        "Matrix.Economy.Domain.Scenarios.ClassicCity";

    [Fact]
    public void ClassicCityNamedTypes_StayInsideClassicCityDomain()
    {
        string[] misplacedTypes = typeof(CityBudgetId).Assembly
           .GetTypes()
           .Where(IsClassicCityNamedType)
           .Where(type => !IsClassicCityDomainType(type))
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

    private static bool IsClassicCityDomainType(Type type)
    {
        return type.Namespace?.StartsWith(
                   value: ScenarioDomainNamespace,
                   comparisonType: StringComparison.Ordinal) == true;
    }
}
