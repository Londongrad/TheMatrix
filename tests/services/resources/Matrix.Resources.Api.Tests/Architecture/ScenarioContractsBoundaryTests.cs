using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Xunit;

namespace Matrix.Resources.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void CityContracts_BelongToClassicCityScenario()
    {
        string[] misplacedTypes = typeof(CityStockpilesView).Assembly
           .GetTypes()
           .Where(type => type.IsPublic)
           .Where(type => type.Name.Contains("City", StringComparison.Ordinal))
           .Where(type =>
                type.Namespace?.StartsWith(
                    "Matrix.Resources.Contracts.Scenarios.ClassicCity",
                    StringComparison.Ordinal) != true)
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }
}
