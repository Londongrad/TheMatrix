using Matrix.Healthcare.Integration.Consumers;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsConsumerDefinitionTests
    {
        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                expected: "healthcare-population-resident-facts-v1",
                actual: PopulationResidentFactsConsumerDefinition.EndpointNameValue);
            Assert.Equal(
                expected: 8,
                actual: PopulationResidentFactsConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
