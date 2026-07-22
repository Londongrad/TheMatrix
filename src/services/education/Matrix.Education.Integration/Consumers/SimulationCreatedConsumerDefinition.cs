using MassTransit;

namespace Matrix.Education.Integration.Consumers;

public sealed class SimulationCreatedConsumerDefinition : ConsumerDefinition<SimulationCreatedConsumer>
{
    public SimulationCreatedConsumerDefinition()
    {
        EndpointName = "education-simulation-created-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SimulationCreatedConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)));
    }
}
