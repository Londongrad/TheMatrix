using MassTransit;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class HealthcareCareDeliveryActivityConsumerDefinition
    : ConsumerDefinition<HealthcareCareDeliveryActivityConsumer>
{
    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public HealthcareCareDeliveryActivityConsumerDefinition()
    {
        EndpointName = "resources-classic-city-healthcare-care-delivery-activity";
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<HealthcareCareDeliveryActivityConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
    }
}
