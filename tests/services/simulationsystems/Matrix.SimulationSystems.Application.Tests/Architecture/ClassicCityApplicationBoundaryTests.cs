using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.SetCityWaterDistributionEmergencyMode;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Architecture;

public sealed class ClassicCityApplicationBoundaryTests
{
    [Fact]
    public void CityApplicationTypes_BelongToClassicCityScenario()
    {
        string[] misplacedTypes = typeof(SetCityWaterDistributionEmergencyModeCommand).Assembly
           .GetTypes()
           .Where(IsClassicCityNamed)
           .Where(type =>
                type.Namespace?.StartsWith(
                    "Matrix.SimulationSystems.Application.Scenarios.ClassicCity",
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
