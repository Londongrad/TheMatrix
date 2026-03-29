using MassTransit;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity
{
    internal static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<CityCreatedConsumer, CityCreatedConsumerDefinition>();
            configurator.AddConsumer<CityOperationalBudgetPressureConsumer, CityOperationalBudgetPressureConsumerDefinition>();
            configurator.AddConsumer<CityStockpileSnapshotConsumer, CityStockpileSnapshotConsumerDefinition>();
            configurator.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherCreatedConsumer, CityWeatherCreatedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherChangedConsumer, CityWeatherChangedConsumerDefinition>();
        }
    }
}
