using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Options;
using Matrix.SimulationSystems.Infrastructure.Outbox;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Integrations.Economy;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationCore;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EconomyPermissionKeys = Matrix.Economy.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;
using SimulationCorePermissionKeys =
    Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityEnvironmentalConditionRepository, CityEnvironmentalConditionRepository>();
            services.AddScoped<ICitySystemsDeletionStateRepository, CitySystemsDeletionStateRepository>();
            services.AddScoped<ICityOperationalExpenseOutboxWriter, CityOperationalExpenseOutboxWriter>();
            services
               .AddScoped<ICityPopulationLivingConditionsOutboxWriter, CityPopulationLivingConditionsOutboxWriter>();
            services.AddScoped<ICitySystemsResourceDemandOutboxWriter, CitySystemsResourceDemandOutboxWriter>();
            services.AddSingleton<IOutboxEventTypeContributor, ClassicCityOutboxEventTypeContributor>();
            services.AddHttpClient<ICityBudgetAuthorizationClient, CityBudgetAuthorizationClient>((
                    sp,
                    client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.Economy))
                        throw new InvalidOperationException("DownstreamServices:Economy is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.Economy,
                        uriKind: UriKind.Absolute);
                })
               .AddInternalServiceAuthentication(
                    identity: InternalServicePrincipals.SimulationSystems,
                    EconomyPermissionKeys.EconomyBudgetAuthorize);
            services.AddHttpClient<ICityMapTopologyClient, CityMapTopologyClient>((
                    sp,
                    client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.SimulationCore))
                        throw new InvalidOperationException("DownstreamServices:SimulationCore is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.SimulationCore,
                        uriKind: UriKind.Absolute);
                })
               .AddInternalServiceAuthentication(
                    identity: InternalServicePrincipals.SimulationSystems,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityRead);
            services.AddHttpClient<ICityOperationalTripDispatcher, CityOperationalTripDispatcher>((
                    sp,
                    client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.SimulationCore))
                        throw new InvalidOperationException("DownstreamServices:SimulationCore is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.SimulationCore,
                        uriKind: UriKind.Absolute);
                })
               .AddInternalServiceAuthentication(
                    identity: InternalServicePrincipals.SimulationSystems,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityRead,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityUpdate);

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
