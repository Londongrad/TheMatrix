using Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;
using Xunit;

namespace Matrix.Education.Integration.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEducationProgressionConsumerDefinitionTests
    {
        [Fact]
        public void EndpointConstants_AreStableAndAllowParallelSimulationHosts()
        {
            Assert.Equal(
                "education-classic-city-progression-v1",
                ClassicCityEducationProgressionConsumerDefinition.EndpointNameValue);
            Assert.Equal(
                8,
                ClassicCityEducationProgressionConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
