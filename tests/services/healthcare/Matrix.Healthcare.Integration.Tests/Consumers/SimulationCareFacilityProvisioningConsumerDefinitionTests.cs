using Matrix.Healthcare.Integration.Consumers;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers;

public sealed class SimulationCareFacilityProvisioningConsumerDefinitionTests
{
    [Fact]
    public void EndpointConstants_AreStableAndBoundConcurrency()
    {
        Assert.Equal(
            "healthcare-simulation-care-facility-provisioning-v1",
            SimulationCareFacilityProvisioningConsumerDefinition.EndpointNameValue);
        Assert.Equal(
            4,
            SimulationCareFacilityProvisioningConsumerDefinition.ConcurrentMessageLimitValue);
    }
}
