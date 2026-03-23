using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherChangedConsumerDefinition : ConsumerDefinition<CityWeatherChangedConsumer>
    {
        public CityWeatherChangedConsumerDefinition()
        {
            EndpointName = "simulation-systems-city-weather-changed";
            ConcurrentMessageLimit = 1;
        }
    }
}
