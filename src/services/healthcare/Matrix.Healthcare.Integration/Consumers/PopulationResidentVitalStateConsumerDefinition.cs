using MassTransit;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentVitalStateConsumerDefinition
        : ConsumerDefinition<PopulationResidentVitalStateConsumer>
    {
        public const string EndpointNameValue = "healthcare-population-resident-vital-state-v1";
        public const int ConcurrentMessageLimitValue = 4;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public PopulationResidentVitalStateConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PopulationResidentVitalStateConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
