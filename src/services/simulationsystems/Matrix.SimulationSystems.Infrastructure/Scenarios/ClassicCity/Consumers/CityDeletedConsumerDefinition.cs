using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumerDefinition : ConsumerDefinition<CityDeletedConsumer>
    {
        public CityDeletedConsumerDefinition()
        {
            EndpointName = "simulation-systems-classic-city-simulation-deleted-v1";
            ConcurrentMessageLimit = 1;
        }
    }
}
