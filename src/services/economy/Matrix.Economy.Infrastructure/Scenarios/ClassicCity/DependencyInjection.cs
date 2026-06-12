using MassTransit;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<CityEconomyCostProfilePolicy>();
            services.AddSingleton<CityHouseholdConsumerSpendAllocationPolicy>();
            services.AddSingleton<CityEconomyServiceQualityPolicy>();
            services.AddSingleton<CityEconomySimulationTemplatePolicy>();

            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<ClassicCityHouseholdAccountSyncConsumer,
                ClassicCityHouseholdAccountSyncConsumerDefinition>();
            configurator.AddConsumer<ClassicCityWorkplaceBusinessSyncConsumer,
                ClassicCityWorkplaceBusinessSyncConsumerDefinition>();
            configurator.AddConsumer<ClassicCityWorkplacePayrollSettlementConsumer,
                ClassicCityWorkplacePayrollSettlementConsumerDefinition>();
            configurator.AddConsumer<ClassicCityHouseholdCashflowSettlementConsumer,
                ClassicCityHouseholdCashflowSettlementConsumerDefinition>();
            configurator.AddConsumer<ClassicCityOperationalExpenseConsumer,
                ClassicCityOperationalExpenseConsumerDefinition>();
        }
    }
}
