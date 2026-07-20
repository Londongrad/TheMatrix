using MassTransit;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;

namespace Matrix.Healthcare.Integration
{
    public static class DependencyInjection
    {
        public static void AddHealthcareIntegrationConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<PopulationResidentFactsConsumer, PopulationResidentFactsConsumerDefinition>();
            configurator.AddConsumer<PopulationResidentVitalStateConsumer,
                PopulationResidentVitalStateConsumerDefinition>();
            configurator.AddConsumer<PopulationResidentHealthRiskConsumer,
                PopulationResidentHealthRiskConsumerDefinition>();
            configurator.AddConsumer<PopulationResidentHealthRiskV2Consumer,
                PopulationResidentHealthRiskV2ConsumerDefinition>();
            configurator.AddConsumer<SimulationCareFacilityProvisioningConsumer,
                SimulationCareFacilityProvisioningConsumerDefinition>();
            configurator.AddConsumer<ClassicCityServiceQualityConsumer,
                ClassicCityServiceQualityConsumerDefinition>();
            configurator.AddConsumer<ClassicCityMedicineSupplyConsumer,
                ClassicCityMedicineSupplyConsumerDefinition>();
            configurator.AddConsumer<SimulationDeletedConsumer, SimulationDeletedConsumerDefinition>();
        }
    }
}
