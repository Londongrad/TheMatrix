using Matrix.SimulationSystems.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Architecture;

public sealed class ClassicCityDomainBoundaryTests
{
    [Fact]
    public void CityDomainTypes_BelongToClassicCityScenario()
    {
        string[] misplacedTypes = typeof(SimulationHostId).Assembly
           .GetTypes()
           .Where(IsClassicCityNamed)
           .Where(type =>
                type.Namespace?.StartsWith(
                    "Matrix.SimulationSystems.Domain.Scenarios.ClassicCity",
                    StringComparison.Ordinal) != true)
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }

    private static bool IsClassicCityNamed(Type type)
    {
        return type.Name.Contains("City", StringComparison.Ordinal);
    }
}
