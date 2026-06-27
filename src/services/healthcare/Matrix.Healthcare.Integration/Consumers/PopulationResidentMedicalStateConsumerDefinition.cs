using MassTransit;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentMedicalStateConsumerDefinition
        : ConsumerDefinition<PopulationResidentMedicalStateConsumer>
    {
        public const string EndpointNameValue = "healthcare-population-resident-medical-state-v1";
        public const int ConcurrentMessageLimitValue = 4;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public PopulationResidentMedicalStateConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PopulationResidentMedicalStateConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
