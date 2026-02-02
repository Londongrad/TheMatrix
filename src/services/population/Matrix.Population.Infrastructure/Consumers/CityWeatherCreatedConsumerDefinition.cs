using MassTransit;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityWeatherCreatedConsumerDefinition : ConsumerDefinition<CityWeatherCreatedConsumer>
    {
        public const string EndpointNameValue = "population-city-weather-created";

        public CityWeatherCreatedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
