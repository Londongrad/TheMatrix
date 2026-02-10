using MassTransit;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityDeletedConsumerDefinition : ConsumerDefinition<CityDeletedConsumer>
    {
        public const string EndpointNameValue = "population-city-deleted";

        public CityDeletedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
