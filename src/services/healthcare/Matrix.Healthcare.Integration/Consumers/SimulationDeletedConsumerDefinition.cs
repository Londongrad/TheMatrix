using MassTransit;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class SimulationDeletedConsumerDefinition
        : ConsumerDefinition<SimulationDeletedConsumer>
    {
        public const string EndpointNameValue = "healthcare-simulation-deleted-v1";
        public const int ConcurrentMessageLimitValue = 4;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public SimulationDeletedConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<SimulationDeletedConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
