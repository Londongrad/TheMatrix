using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumerDefinition : ConsumerDefinition<CityTimeAdvancedConsumer>
    {
        public CityTimeAdvancedConsumerDefinition()
        {
            EndpointName = "population-classic-city-tick-phase-reached-v1";
            ConcurrentMessageLimit = 1;
        }
    }
}
