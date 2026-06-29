using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class HealthcarePatientHealthOutcomeConsumerDefinition
        : ConsumerDefinition<HealthcarePatientHealthOutcomeConsumer>
    {
        public const string EndpointNameValue = "population-healthcare-patient-health-outcome-v1";
        public const int ConcurrentMessageLimitValue = 1;

        public HealthcarePatientHealthOutcomeConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }
    }
}
