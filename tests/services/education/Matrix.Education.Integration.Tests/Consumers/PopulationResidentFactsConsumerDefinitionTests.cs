using Matrix.Education.Integration.Consumers;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsConsumerDefinitionTests
    {
        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                expected: "education-population-resident-facts-v1",
                actual: PopulationResidentFactsConsumerDefinition.EndpointNameValue);
            Assert.Equal(
                expected: 8,
                actual: PopulationResidentFactsConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
