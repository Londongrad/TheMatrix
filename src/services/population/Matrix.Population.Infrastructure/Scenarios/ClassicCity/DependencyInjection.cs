using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityPopulationPersonReadRepository, CityPopulationPersonReadRepository>();
            services.AddScoped<IHouseholdWriteRepository, HouseholdWriteRepository>();
            services.AddScoped<ICityPopulationArchiveStateRepository, CityPopulationArchiveStateRepository>();
            services.AddScoped<ICityPopulationDeletionStateRepository, CityPopulationDeletionStateRepository>();
            services.AddScoped<ICityPopulationEnvironmentRepository, CityPopulationEnvironmentRepository>();
            services.AddScoped<ICityPopulationProgressionStateRepository, CityPopulationProgressionStateRepository>();
            services.AddScoped<ICityPopulationSummaryReadRepository, CityPopulationSummaryReadRepository>();
            services.AddScoped<ICityPopulationWeatherImpactStateRepository, CityPopulationWeatherImpactStateRepository>();
            services.AddScoped<ICityPopulationWeatherExposureStateRepository,
                CityPopulationWeatherExposureStateRepository>();

            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<CityArchivedConsumer, CityArchivedConsumerDefinition>();
            configurator.AddConsumer<CityDeletedConsumer, CityDeletedConsumerDefinition>();
            configurator.AddConsumer<CityEnvironmentChangedConsumer, CityEnvironmentChangedConsumerDefinition>();
            configurator.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherCreatedConsumer, CityWeatherCreatedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherChangedConsumer, CityWeatherChangedConsumerDefinition>();
        }
    }
}
