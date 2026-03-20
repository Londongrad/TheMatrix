using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityServiceQualitySnapshotConsumerDefinition
        : ConsumerDefinition<ClassicCityServiceQualitySnapshotConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-service-quality";

        public ClassicCityServiceQualitySnapshotConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
