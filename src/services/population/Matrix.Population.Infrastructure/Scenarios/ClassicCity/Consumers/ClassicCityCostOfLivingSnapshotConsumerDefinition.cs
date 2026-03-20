using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityCostOfLivingSnapshotConsumerDefinition
        : ConsumerDefinition<ClassicCityCostOfLivingSnapshotConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-cost-of-living";

        public ClassicCityCostOfLivingSnapshotConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
