using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumerDefinition : ConsumerDefinition<CityCreatedConsumer>
    {
        public CityCreatedConsumerDefinition()
        {
            EndpointName = "simulation-systems-city-created";
            ConcurrentMessageLimit = 1;
        }
    }
}
