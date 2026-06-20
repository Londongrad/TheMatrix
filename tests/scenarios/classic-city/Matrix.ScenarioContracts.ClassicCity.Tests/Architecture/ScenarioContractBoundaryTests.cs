using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using NetArchTest.Rules;
using Xunit;

namespace Matrix.ScenarioContracts.ClassicCity.Tests.Architecture
{
    public sealed class ScenarioContractBoundaryTests
    {
        private const string ContractNamespace =
            "Matrix.ScenarioContracts.ClassicCity.IntegrationEvents";

        [Fact]
        public void PublicTypes_BelongToClassicCityIntegrationEventNamespace()
        {
            string[] misplacedTypes = typeof(CityEconomyDailySettlementV1).Assembly
               .GetExportedTypes()
               .Where(type =>
                    type.Namespace?.StartsWith(
                        ContractNamespace,
                        StringComparison.Ordinal) != true)
               .Select(type => type.FullName ?? type.Name)
               .Order(StringComparer.Ordinal)
               .ToArray();

            Assert.Empty(misplacedTypes);
        }

        [Fact]
        public void Contracts_DoNotDependOnBuildingBlocksOrServices()
        {
            TestResult result = Types
               .InAssembly(typeof(CityEconomyDailySettlementV1).Assembly)
               .ShouldNot()
               .HaveDependencyOnAny(
                    "Matrix.BuildingBlocks",
                    "Matrix.Economy",
                    "Matrix.Population",
                    "Matrix.Resources",
                    "Matrix.SimulationCore",
                    "Matrix.SimulationSystems")
               .GetResult();

            string dependencies = result.FailingTypes is null
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    result.FailingTypes
                       .Select(type => type.FullName)
                       .Where(typeName => typeName is not null)
                       .Order());

            Assert.True(result.IsSuccessful, dependencies);
        }
    }
}
