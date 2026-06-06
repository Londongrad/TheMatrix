using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Xunit;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Population
{
    public sealed class PopulationClientContractTests
    {
        [Fact]
        public void SharedPopulationContract_ExcludesClassicCityOperations()
        {
            string[] sharedMethods = typeof(IPopulationApiClient)
               .GetMethods()
               .Select(method => method.Name)
               .Order()
               .ToArray();
            string[] scenarioMethods = typeof(IClassicCityPopulationApiClient)
               .GetMethods()
               .Select(method => method.Name)
               .ToArray();

            Assert.Equal(
                expected: [nameof(IPopulationApiClient.GetCitizensPageAsync)],
                actual: sharedMethods);
            Assert.DoesNotContain(
                expected: nameof(IPopulationApiClient.GetCitizensPageAsync),
                collection: scenarioMethods);
            Assert.Contains(
                expected: nameof(IClassicCityPopulationApiClient.GetCityResidentsPageAsync),
                collection: scenarioMethods);
        }
    }
}
