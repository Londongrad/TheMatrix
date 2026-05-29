using MassTransit;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class SimulationDeletedConsumerDefinition : ConsumerDefinition<SimulationDeletedConsumer>
{
    public SimulationDeletedConsumerDefinition()
    {
        EndpointName = "resources-classic-city-simulation-deleted-v1";
        ConcurrentMessageLimit = 1;
    }
}
