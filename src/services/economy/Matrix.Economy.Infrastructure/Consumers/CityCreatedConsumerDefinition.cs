using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityCreatedConsumerDefinition : ConsumerDefinition<CityCreatedConsumer>
    {
        public CityCreatedConsumerDefinition()
        {
            EndpointName = "economy-city-created";
            ConcurrentMessageLimit = 1;
        }
    }
}
