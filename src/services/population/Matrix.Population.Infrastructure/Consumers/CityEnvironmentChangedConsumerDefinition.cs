using MassTransit;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityEnvironmentChangedConsumerDefinition : ConsumerDefinition<CityEnvironmentChangedConsumer>
    {
        public const string EndpointNameValue = "population-city-environment-changed";

        public CityEnvironmentChangedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
