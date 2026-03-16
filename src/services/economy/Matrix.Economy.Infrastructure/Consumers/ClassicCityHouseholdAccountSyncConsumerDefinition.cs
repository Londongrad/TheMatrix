using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class ClassicCityHouseholdAccountSyncConsumerDefinition
        : ConsumerDefinition<ClassicCityHouseholdAccountSyncConsumer>
    {
        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public ClassicCityHouseholdAccountSyncConsumerDefinition()
        {
            EndpointName = "economy-classic-city-household-account-sync";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ClassicCityHouseholdAccountSyncConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
