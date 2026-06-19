using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Population.Api.Tests.Architecture;

public sealed class ScenarioContractsBoundaryTests
{
    [Fact]
    public void CityContracts_BelongToClassicCityScenario()
    {
        string[] misplacedTypes = typeof(CityResidentDetailsDto).Assembly
           .GetTypes()
           .Where(type => type.IsPublic)
           .Where(type =>
                type.Name.StartsWith("City", StringComparison.Ordinal) ||
                type.Name.Contains("ClassicCity", StringComparison.Ordinal))
           .Where(type =>
                type.Namespace?.StartsWith(
                    "Matrix.Population.Contracts.Scenarios.ClassicCity",
                    StringComparison.Ordinal) != true)
           .Select(type => type.FullName ?? type.Name)
           .Order(StringComparer.Ordinal)
           .ToArray();

        Assert.Empty(misplacedTypes);
    }
}
