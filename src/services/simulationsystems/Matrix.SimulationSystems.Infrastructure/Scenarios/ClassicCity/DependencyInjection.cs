using MassTransit;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Outbox;
using Matrix.SimulationSystems.Infrastructure.Persistence.Repositories;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity
{
    internal static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityEnvironmentalConditionRepository, CityEnvironmentalConditionRepository>();
            services.AddScoped<ICitySystemsDeletionStateRepository, CitySystemsDeletionStateRepository>();
            services.AddScoped<ICityOperationalExpenseOutboxWriter, CityOperationalExpenseOutboxWriter>();
            services
               .AddScoped<ICityPopulationLivingConditionsOutboxWriter, CityPopulationLivingConditionsOutboxWriter>();
            services.AddScoped<ICitySystemsResourceDemandOutboxWriter, CitySystemsResourceDemandOutboxWriter>();

            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<CityCreatedConsumer, CityCreatedConsumerDefinition>();
            configurator.AddConsumer<CityDeletedConsumer, CityDeletedConsumerDefinition>();
            configurator
               .AddConsumer<CityOperationalBudgetPressureConsumer, CityOperationalBudgetPressureConsumerDefinition>();
            configurator.AddConsumer<CityStockpileSnapshotConsumer, CityStockpileSnapshotConsumerDefinition>();
            configurator.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherCreatedConsumer, CityWeatherCreatedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherChangedConsumer, CityWeatherChangedConsumerDefinition>();
        }
    }
}
