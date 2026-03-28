using MassTransit;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity
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
            configurator.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
            configurator.AddConsumer<CitySystemsResourceDemandConsumer, CitySystemsResourceDemandConsumerDefinition>();
        }
    }
}
