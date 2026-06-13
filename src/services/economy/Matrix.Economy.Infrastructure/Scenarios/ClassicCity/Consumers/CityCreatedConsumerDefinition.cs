using MassTransit;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumerDefinition : ConsumerDefinition<CityCreatedConsumer>
    {
        public CityCreatedConsumerDefinition()
        {
            EndpointName = "economy-classic-city-created-v1";
            ConcurrentMessageLimit = 1;
        }
    }
}
