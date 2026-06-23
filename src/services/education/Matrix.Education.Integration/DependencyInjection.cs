using MassTransit;
using Matrix.Education.Integration.Consumers;

namespace Matrix.Education.Integration
{
    public static class DependencyInjection
    {
        public static void AddEducationIntegrationConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<PopulationResidentFactsConsumer, PopulationResidentFactsConsumerDefinition>();
        }
    }
}
