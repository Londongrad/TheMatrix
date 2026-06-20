using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity;
using Xunit;

namespace Matrix.SimulationSystems.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void CityContracts_BelongToClassicCityScenario()
    {
        string[] misplacedTypes = typeof(ClassicCitySimulationSystemsApiRoutes).Assembly
           .GetTypes()
           .Where(type => type.IsPublic)
           .Where(type => type.Name.Contains("City", StringComparison.Ordinal))
           .Where(type =>
                type.Namespace?.StartsWith(
                    "Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity",
                    StringComparison.Ordinal) != true)
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }
}
