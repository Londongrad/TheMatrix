using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Xunit;

namespace Matrix.Resources.Application.Tests.Architecture;

public sealed class ClassicCityApplicationBoundaryTests
{
    [Fact]
    public void CityApplicationTypes_BelongToClassicCityScenario()
    {
        string[] misplacedTypes = typeof(SyncCitySystemsDemandCommand).Assembly
           .GetTypes()
           .Where(IsClassicCityNamed)
           .Where(type =>
                type.Namespace?.StartsWith(
                    "Matrix.Resources.Application.Scenarios.ClassicCity",
                    StringComparison.Ordinal) != true)
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }

    private static bool IsClassicCityNamed(Type type)
    {
        return type.Name.StartsWith("City", StringComparison.Ordinal) ||
               type.Name.StartsWith("ICity", StringComparison.Ordinal) ||
               type.Name.Contains("ClassicCity", StringComparison.Ordinal);
    }
}
