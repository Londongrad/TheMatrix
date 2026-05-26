using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityDeletedConsumerDefinition : ConsumerDefinition<CityDeletedConsumer>
    {
        public CityDeletedConsumerDefinition()
        {
            EndpointName = "economy-city-deleted";
            ConcurrentMessageLimit = 1;
        }
    }
}
