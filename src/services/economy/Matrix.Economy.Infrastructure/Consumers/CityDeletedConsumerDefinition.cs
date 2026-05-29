using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityDeletedConsumerDefinition : ConsumerDefinition<CityDeletedConsumer>
    {
        public CityDeletedConsumerDefinition()
        {
            EndpointName = "economy-classic-city-simulation-deleted-v1";
            ConcurrentMessageLimit = 1;
        }
    }
}
