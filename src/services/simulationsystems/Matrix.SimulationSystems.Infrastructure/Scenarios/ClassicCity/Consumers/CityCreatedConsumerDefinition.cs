using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumerDefinition : ConsumerDefinition<CityCreatedConsumer>
    {
        public CityCreatedConsumerDefinition()
        {
            EndpointName = "simulation-systems-classic-city-created-v1";
            ConcurrentMessageLimit = 1;
        }
    }
}
