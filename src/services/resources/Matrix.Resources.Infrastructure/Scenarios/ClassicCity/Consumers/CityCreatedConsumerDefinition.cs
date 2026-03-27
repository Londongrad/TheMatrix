using MassTransit;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumerDefinition : ConsumerDefinition<CityCreatedConsumer>
    {
        public CityCreatedConsumerDefinition()
        {
            EndpointName = "resources-city-created";
            ConcurrentMessageLimit = 1;
        }
    }
}
