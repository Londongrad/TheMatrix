using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityStockpileSnapshotConsumerDefinition
        : ConsumerDefinition<ClassicCityStockpileSnapshotConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-stockpiles";

        public ClassicCityStockpileSnapshotConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
