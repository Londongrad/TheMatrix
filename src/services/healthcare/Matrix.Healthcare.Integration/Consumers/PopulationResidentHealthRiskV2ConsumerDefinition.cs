using MassTransit;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentHealthRiskV2ConsumerDefinition
        : ConsumerDefinition<PopulationResidentHealthRiskV2Consumer>
    {
        public const string EndpointNameValue = "healthcare-population-resident-health-risk-v2";
        public const int ConcurrentMessageLimitValue = 4;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public PopulationResidentHealthRiskV2ConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PopulationResidentHealthRiskV2Consumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
