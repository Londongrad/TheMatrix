using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class ClassicCityHouseholdAccountSyncConsumerDefinition
        : ConsumerDefinition<ClassicCityHouseholdAccountSyncConsumer>
    {
        public ClassicCityHouseholdAccountSyncConsumerDefinition()
        {
            EndpointName = "economy-classic-city-household-account-sync";
            ConcurrentMessageLimit = 1;
        }
    }
}
