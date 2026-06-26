using MassTransit;
using Matrix.Healthcare.Integration.Consumers;

namespace Matrix.Healthcare.Integration
{
    public static class DependencyInjection
    {
        public static void AddHealthcareIntegrationConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<PopulationResidentFactsConsumer, PopulationResidentFactsConsumerDefinition>();
            configurator.AddConsumer<SimulationDeletedConsumer, SimulationDeletedConsumerDefinition>();
        }
    }
}
