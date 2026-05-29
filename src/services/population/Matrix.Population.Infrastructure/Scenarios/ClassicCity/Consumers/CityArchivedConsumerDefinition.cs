using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityArchivedConsumerDefinition : ConsumerDefinition<CityArchivedConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-simulation-archived-v1";

        public CityArchivedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
