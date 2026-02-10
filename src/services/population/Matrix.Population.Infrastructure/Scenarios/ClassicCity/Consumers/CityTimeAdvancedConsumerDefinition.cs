using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumerDefinition : ConsumerDefinition<CityTimeAdvancedConsumer>
    {
        public CityTimeAdvancedConsumerDefinition()
        {
            EndpointName = "population-city-time-advanced";
            ConcurrentMessageLimit = 1;
        }
    }
}
