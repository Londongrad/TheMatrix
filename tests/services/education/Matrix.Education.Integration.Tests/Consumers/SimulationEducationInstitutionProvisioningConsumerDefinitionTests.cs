using Matrix.Education.Integration.Consumers;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers;

public sealed class SimulationEducationInstitutionProvisioningConsumerDefinitionTests
{
    [Fact]
    public void EndpointConstants_AreStableAndBoundConcurrency()
    {
        Assert.Equal(
            expected: "education-simulation-institution-provisioning-v1",
            actual: SimulationEducationInstitutionProvisioningConsumerDefinition.EndpointNameValue);
        Assert.Equal(
            expected: 4,
            actual: SimulationEducationInstitutionProvisioningConsumerDefinition.ConcurrentMessageLimitValue);
    }
}
