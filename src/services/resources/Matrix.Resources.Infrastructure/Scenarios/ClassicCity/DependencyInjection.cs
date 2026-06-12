using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity
{
    internal static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityStockpileRepository, CityStockpileRepository>();
            services.AddScoped<ICityResourceDeletionStateRepository, CityResourceDeletionStateRepository>();

            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<CityCreatedConsumer, CityCreatedConsumerDefinition>();
            configurator.AddConsumer<SimulationDeletedConsumer, SimulationDeletedConsumerDefinition>();
            configurator
               .AddConsumer<SimulationTickPhaseReachedConsumer, SimulationTickPhaseReachedConsumerDefinition>();
            configurator
               .AddConsumer<CityOperationalBudgetPressureConsumer, CityOperationalBudgetPressureConsumerDefinition>();
            configurator.AddConsumer<CitySystemsResourceDemandConsumer, CitySystemsResourceDemandConsumerDefinition>();
        }
    }
}
