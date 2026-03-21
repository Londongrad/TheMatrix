using MassTransit;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityWorkplaceBusinessSyncConsumerDefinition
        : ConsumerDefinition<ClassicCityWorkplaceBusinessSyncConsumer>
    {
        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public ClassicCityWorkplaceBusinessSyncConsumerDefinition()
        {
            EndpointName = "economy-classic-city-workplace-business-sync";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ClassicCityWorkplaceBusinessSyncConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
