using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Infrastructure.Options;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Integrations.Economy;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationCore;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EconomyPermissionKeys = Matrix.Economy.Contracts.Authorization.Permissions.PermissionKeys;
using SimulationCorePermissionKeys = Matrix.SimulationCore.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityStockpileRepository, CityStockpileRepository>();
            services.AddScoped<ICityResourceDeletionStateRepository, CityResourceDeletionStateRepository>();
            services.AddScoped<ICityStockpileSnapshotOutboxWriter, CityStockpileSnapshotOutboxWriter>();
            services.AddScoped<ICityOperationalExpenseOutboxWriter, CityOperationalExpenseOutboxWriter>();
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
                    identity: InternalServicePrincipals.Resources,
                    EconomyPermissionKeys.EconomyBudgetAuthorize);
            services.AddHttpClient<ICityResupplyTripDispatcher, CityResupplyTripDispatcher>((
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
                    identity: InternalServicePrincipals.Resources,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityRead,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityUpdate);

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
