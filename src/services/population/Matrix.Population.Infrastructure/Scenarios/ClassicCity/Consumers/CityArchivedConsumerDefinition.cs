using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityArchivedConsumerDefinition : ConsumerDefinition<CityArchivedConsumer>
    {
        public const string EndpointNameValue = "population-city-archived";

        public CityArchivedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
