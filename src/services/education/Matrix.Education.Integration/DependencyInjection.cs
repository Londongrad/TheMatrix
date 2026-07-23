using MassTransit;
using Matrix.Education.Integration.Consumers;
using Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;

namespace Matrix.Education.Integration
{
    public static class DependencyInjection
    {
        public static void AddEducationIntegrationConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<ClassicCityLearningAttendanceConsumer, ClassicCityLearningAttendanceConsumerDefinition>();
            configurator.AddConsumer<PopulationResidentFactsConsumer, PopulationResidentFactsConsumerDefinition>();
            configurator.AddConsumer<SimulationEducationInstitutionProvisioningConsumer,
                SimulationEducationInstitutionProvisioningConsumerDefinition>();
            configurator.AddConsumer<SimulationDeletedConsumer, SimulationDeletedConsumerDefinition>();
            configurator.AddConsumer<SimulationCreatedConsumer, SimulationCreatedConsumerDefinition>();
            configurator.AddConsumer<ClassicCityEducationProgressionConsumer,
                ClassicCityEducationProgressionConsumerDefinition>();
        }
    }
}
