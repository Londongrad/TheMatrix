using MassTransit;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class SimulationTickPhaseReachedConsumerDefinition
    : ConsumerDefinition<SimulationTickPhaseReachedConsumer>
{
    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public SimulationTickPhaseReachedConsumerDefinition()
    {
        EndpointName = "resources-classic-city-tick-phase-reached-v1";
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SimulationTickPhaseReachedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
    }
}
