using MassTransit;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentHealthRiskConsumerDefinition
        : ConsumerDefinition<PopulationResidentHealthRiskConsumer>
    {
        public const string EndpointNameValue = "healthcare-population-resident-health-risk-v1";
        public const int ConcurrentMessageLimitValue = 4;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public PopulationResidentHealthRiskConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PopulationResidentHealthRiskConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
