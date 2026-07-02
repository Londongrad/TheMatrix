using MassTransit;

namespace Matrix.Healthcare.Integration.Consumers;

public sealed class SimulationCareFacilityProvisioningConsumerDefinition
    : ConsumerDefinition<SimulationCareFacilityProvisioningConsumer>
{
    public const string EndpointNameValue =
        "healthcare-simulation-care-facility-provisioning-v1";
    public const int ConcurrentMessageLimitValue = 4;

    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public SimulationCareFacilityProvisioningConsumerDefinition()
    {
        EndpointName = EndpointNameValue;
        ConcurrentMessageLimit = ConcurrentMessageLimitValue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SimulationCareFacilityProvisioningConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
    }
}
