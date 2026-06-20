using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.Architecture
{
    public sealed class ScenarioIntegrationEventBoundaryTests
    {
        [Fact]
        public void CityIntegrationEvents_BelongToClassicCityScenario()
        {
            Type[] misplacedTypes = typeof(CityEconomyDailySettlementV1).Assembly
               .GetTypes()
               .Where(type =>
                    type.IsPublic &&
                    type.Namespace?.StartsWith(
                        "Matrix.BuildingBlocks.Application.IntegrationEvents.",
                        StringComparison.Ordinal) == true &&
                    (type.Name.StartsWith("City", StringComparison.Ordinal) ||
                     type.Name.Contains("ClassicCity", StringComparison.Ordinal)) &&
                    !type.Namespace.StartsWith(
                        "Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.",
                        StringComparison.Ordinal))
               .ToArray();

            Assert.Empty(misplacedTypes);
        }
    }
}
