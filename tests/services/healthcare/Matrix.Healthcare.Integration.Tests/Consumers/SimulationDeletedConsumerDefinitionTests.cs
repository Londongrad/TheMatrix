using Matrix.Healthcare.Integration.Consumers;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class SimulationDeletedConsumerDefinitionTests
    {
        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                expected: "healthcare-simulation-deleted-v1",
                actual: SimulationDeletedConsumerDefinition.EndpointNameValue);
            Assert.Equal(
                expected: 4,
                actual: SimulationDeletedConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
