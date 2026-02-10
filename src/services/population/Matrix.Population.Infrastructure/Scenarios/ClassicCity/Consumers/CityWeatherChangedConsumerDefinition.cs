using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherChangedConsumerDefinition : ConsumerDefinition<CityWeatherChangedConsumer>
    {
        public const string EndpointNameValue = "population-city-weather-changed";

        public CityWeatherChangedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
