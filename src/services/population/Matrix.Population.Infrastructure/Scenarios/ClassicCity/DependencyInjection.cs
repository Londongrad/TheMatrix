using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Infrastructure.Options;
using Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationCore;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationSystems;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimulationCorePermissionKeys =
    Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityPopulationPersonReadRepository, CityPopulationPersonReadRepository>();
            services.AddScoped<ICityEconomySettlementOutboxWriter, CityEconomySettlementOutboxWriter>();
            services.AddHttpClient<ICityRouteResolutionClient, CityRouteResolutionClient>((
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
                    identity: InternalServicePrincipals.Population,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityRead);
            services.AddHttpClient<ICityPopulationActiveTripClient, CityActiveTripClient>((
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
                    identity: InternalServicePrincipals.Population,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityRead,
                    SimulationCorePermissionKeys.SimulationCoreClassicCityUpdate);
            services.AddHttpClient<ICityDistrictUtilityConditionsClient, CityDistrictUtilityConditionsClient>((
                    sp,
                    client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.SimulationSystems))
                        throw new InvalidOperationException("DownstreamServices:SimulationSystems is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.SimulationSystems,
                        uriKind: UriKind.Absolute);
                })
               .AddInternalServiceAuthentication(identity: InternalServicePrincipals.Population);
            services.AddScoped<IHouseholdWriteRepository, HouseholdWriteRepository>();
            services.AddScoped<ICityPopulationArchiveStateRepository, CityPopulationArchiveStateRepository>();
            services.AddScoped<ICityPopulationActivityJournalService, CityPopulationActivityJournalService>();
            services.AddScoped<ICityPopulationAnchorCatalogRepository, CityPopulationAnchorCatalogRepository>();
            services.AddScoped<ICityPopulationCostOfLivingStateRepository, CityPopulationCostOfLivingStateRepository>();
            services.AddScoped<ICityPopulationDashboardReadRepository, CityPopulationDashboardReadRepository>();
            services.AddScoped<ICityPopulationDeletionStateRepository, CityPopulationDeletionStateRepository>();
            services.AddScoped<ICityPopulationEmployerFinancialStressStateRepository,
                CityPopulationEmployerFinancialStressStateRepository>();
            services.AddScoped<ICityPopulationEssentialsStateRepository, CityPopulationEssentialsStateRepository>();
            services.AddScoped<ICityPopulationEnvironmentRepository, CityPopulationEnvironmentRepository>();
            services.AddScoped<ICityPopulationHouseholdFinancialStressStateRepository,
                CityPopulationHouseholdFinancialStressStateRepository>();
            services
               .AddScoped<ICityPopulationLivingConditionsStateRepository,
                    CityPopulationLivingConditionsStateRepository>();
            services.AddScoped<ICityPopulationProgressionStateRepository, CityPopulationProgressionStateRepository>();
            services
               .AddScoped<ICityPopulationServiceQualityStateRepository, CityPopulationServiceQualityStateRepository>();
            services.AddScoped<ICityPopulationSummaryProjectionService, CityPopulationSummaryProjectionService>();
            services.AddScoped<ICityPopulationSummaryReadRepository, CityPopulationSummaryReadRepository>();
            services
               .AddScoped<ICityPopulationWeatherImpactStateRepository, CityPopulationWeatherImpactStateRepository>();
            services.AddScoped<ICityPopulationWeatherExposureStateRepository,
                CityPopulationWeatherExposureStateRepository>();

            return services;
        }

        public static void AddClassicCityScenarioConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<ClassicCityCostOfLivingSnapshotConsumer,
                ClassicCityCostOfLivingSnapshotConsumerDefinition>();
            configurator.AddConsumer<ClassicCityLivingConditionsSnapshotConsumer,
                ClassicCityLivingConditionsSnapshotConsumerDefinition>();
            configurator.AddConsumer<ClassicCityServiceQualitySnapshotConsumer,
                ClassicCityServiceQualitySnapshotConsumerDefinition>();
            configurator.AddConsumer<ClassicCityStockpileSnapshotConsumer,
                ClassicCityStockpileSnapshotConsumerDefinition>();
            configurator.AddConsumer<ClassicCityEmployerFinancialStressConsumer,
                ClassicCityEmployerFinancialStressConsumerDefinition>();
            configurator.AddConsumer<ClassicCityHouseholdFinancialStressConsumer,
                ClassicCityHouseholdFinancialStressConsumerDefinition>();
            configurator.AddConsumer<CityArchivedConsumer, CityArchivedConsumerDefinition>();
            configurator.AddConsumer<CityDeletedConsumer, CityDeletedConsumerDefinition>();
            configurator.AddConsumer<CityEnvironmentChangedConsumer, CityEnvironmentChangedConsumerDefinition>();
            configurator.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherCreatedConsumer, CityWeatherCreatedConsumerDefinition>();
            configurator.AddConsumer<CityWeatherChangedConsumer, CityWeatherChangedConsumerDefinition>();
        }
    }
}
