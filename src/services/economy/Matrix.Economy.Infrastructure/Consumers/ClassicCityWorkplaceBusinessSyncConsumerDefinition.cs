using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class ClassicCityWorkplaceBusinessSyncConsumerDefinition
        : ConsumerDefinition<ClassicCityWorkplaceBusinessSyncConsumer>
    {
        public ClassicCityWorkplaceBusinessSyncConsumerDefinition()
        {
            EndpointName = "economy-classic-city-workplace-business-sync";
            ConcurrentMessageLimit = 1;
        }
    }
}
