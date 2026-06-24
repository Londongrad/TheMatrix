using Matrix.Education.Integration.Consumers;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers
{
    public sealed class SimulationDeletedConsumerDefinitionTests
    {
        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                expected: "education-simulation-deleted-v1",
                actual: SimulationDeletedConsumerDefinition.EndpointNameValue);
            Assert.Equal(
                expected: 4,
                actual: SimulationDeletedConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
