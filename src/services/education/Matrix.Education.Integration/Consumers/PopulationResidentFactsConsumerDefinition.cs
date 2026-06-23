using MassTransit;

namespace Matrix.Education.Integration.Consumers
{
    public sealed class PopulationResidentFactsConsumerDefinition
        : ConsumerDefinition<PopulationResidentFactsConsumer>
    {
        public const string EndpointNameValue = "education-population-resident-facts-v1";
        public const int ConcurrentMessageLimitValue = 8;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public PopulationResidentFactsConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PopulationResidentFactsConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
