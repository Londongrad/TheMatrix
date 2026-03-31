using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityLivingConditionsSnapshotConsumerDefinition
        : ConsumerDefinition<ClassicCityLivingConditionsSnapshotConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-living-conditions";

        public ClassicCityLivingConditionsSnapshotConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
