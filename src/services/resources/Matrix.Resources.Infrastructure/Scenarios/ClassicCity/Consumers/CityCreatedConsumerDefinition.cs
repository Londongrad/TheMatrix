using MassTransit;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumerDefinition : ConsumerDefinition<CityCreatedConsumer>
    {
        public CityCreatedConsumerDefinition()
        {
            EndpointName = "resources-classic-city-created-v1";
            ConcurrentMessageLimit = 1;
        }
    }
}
