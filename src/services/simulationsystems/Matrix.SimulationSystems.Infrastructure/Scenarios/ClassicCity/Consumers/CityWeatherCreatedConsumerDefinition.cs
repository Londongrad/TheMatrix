using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherCreatedConsumerDefinition : ConsumerDefinition<CityWeatherCreatedConsumer>
    {
        public CityWeatherCreatedConsumerDefinition()
        {
            EndpointName = "simulation-systems-city-weather-created";
            ConcurrentMessageLimit = 1;
        }
    }
}
