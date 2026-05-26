using MassTransit;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumerDefinition : ConsumerDefinition<CityDeletedConsumer>
    {
        public CityDeletedConsumerDefinition()
        {
            EndpointName = "resources-city-deleted";
            ConcurrentMessageLimit = 1;
        }
    }
}
