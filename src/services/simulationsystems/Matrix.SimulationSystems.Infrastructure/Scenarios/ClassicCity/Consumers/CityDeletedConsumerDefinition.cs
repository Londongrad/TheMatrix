using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumerDefinition : ConsumerDefinition<CityDeletedConsumer>
    {
        public CityDeletedConsumerDefinition()
        {
            EndpointName = "simulation-systems-city-deleted";
            ConcurrentMessageLimit = 1;
        }
    }
}
