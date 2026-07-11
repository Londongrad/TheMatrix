using MassTransit;

namespace Matrix.Education.Integration.Consumers;

public sealed class SimulationEducationInstitutionProvisioningConsumerDefinition
    : ConsumerDefinition<SimulationEducationInstitutionProvisioningConsumer>
{
    public const string EndpointNameValue =
        "education-simulation-institution-provisioning-v1";
    public const int ConcurrentMessageLimitValue = 4;

    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public SimulationEducationInstitutionProvisioningConsumerDefinition()
    {
        EndpointName = EndpointNameValue;
        ConcurrentMessageLimit = ConcurrentMessageLimitValue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SimulationEducationInstitutionProvisioningConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
    }
}
