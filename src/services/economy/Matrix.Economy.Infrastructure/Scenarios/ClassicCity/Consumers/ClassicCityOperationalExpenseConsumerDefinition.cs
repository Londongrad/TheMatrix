using MassTransit;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityOperationalExpenseConsumerDefinition
        : ConsumerDefinition<ClassicCityOperationalExpenseConsumer>
    {
        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public ClassicCityOperationalExpenseConsumerDefinition()
        {
            EndpointName = "economy-classic-city-operational-expense";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ClassicCityOperationalExpenseConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
