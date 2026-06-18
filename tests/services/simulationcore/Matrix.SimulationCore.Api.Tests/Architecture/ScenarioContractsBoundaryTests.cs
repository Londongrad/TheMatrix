using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Xunit;

namespace Matrix.SimulationCore.Api.Tests.Architecture
{
    public sealed class ScenarioContractsBoundaryTests
    {
        [Fact]
        public void CityContracts_BelongToClassicCityScenario()
        {
            Type[] misplacedTypes = typeof(ClassicCityCreatedV1).Assembly
               .GetTypes()
               .Where(type =>
                    type.IsPublic &&
                    (type.Name.StartsWith("City", StringComparison.Ordinal) ||
                     type.Name.Contains("ClassicCity", StringComparison.Ordinal)) &&
                    (type.Namespace is null ||
                     !type.Namespace.StartsWith(
                         "Matrix.SimulationCore.Contracts.Scenarios.ClassicCity",
                         StringComparison.Ordinal)))
               .ToArray();

            Assert.Empty(misplacedTypes);
        }
    }
}
