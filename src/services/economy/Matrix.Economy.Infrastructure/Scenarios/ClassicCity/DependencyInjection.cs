using MassTransit;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Infrastructure.Outbox;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Services;
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
            services.AddScoped<ICityBudgetRepository, CityBudgetRepository>();
            services.AddScoped<ICityBudgetAllocationRepository, CityBudgetAllocationRepository>();
            services.AddScoped<ICityBudgetLedgerRepository, CityBudgetLedgerRepository>();
            services.AddScoped<ICityBudgetSettlementRepository, CityBudgetSettlementRepository>();
            services.AddScoped<ICityOperationalBudgetSignalPublisher, CityOperationalBudgetSignalOutboxWriter>();
            services.AddScoped<ICityPopulationSignalPublisher, CityPopulationSignalOutboxWriter>();
            services.AddScoped<ICityEconomyBootstrapService, CityEconomyBootstrapService>();
            services.AddSingleton<IOutboxEventTypeContributor, ClassicCityOutboxEventTypeContributor>();

            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<CityCreatedConsumer, CityCreatedConsumerDefinition>();
            configurator.AddConsumer<CityDeletedConsumer, CityDeletedConsumerDefinition>();
            configurator.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
            configurator
               .AddConsumer<CityEconomyDailySettlementConsumer, CityEconomyDailySettlementConsumerDefinition>();
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
