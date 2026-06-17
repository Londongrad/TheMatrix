using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Infrastructure.Options;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Integrations.Economy;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Integrations.Population;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationSystems;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Provisioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EconomyPermissionKeys = Matrix.Economy.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;
using PopulationPermissionKeys = Matrix.Population.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IDistrictRepository, DistrictRepository>();
            services.AddScoped<IResidentialBuildingRepository, ResidentialBuildingRepository>();
            services.AddScoped<ICityAnchorRepository, CityAnchorRepository>();
            services.AddScoped<IRoadNodeRepository, RoadNodeRepository>();
            services.AddScoped<IRoadSegmentRepository, RoadSegmentRepository>();
            services.AddScoped<ICityActiveTripRepository, CityActiveTripRepository>();
            services.AddScoped<ICityWeatherRepository, CityWeatherRepository>();
            services.AddScoped<IClassicCityOutboxWriter, ClassicCityOutboxWriter>();
            services.AddSingleton<IOutboxEventTypeContributor, ClassicCityOutboxEventTypeContributor>();

            services.AddOptions<ProvisioningRecoveryOptions>()
               .Bind(configuration.GetSection(ProvisioningRecoveryOptions.SectionName));
            services.AddHostedService<CityProvisioningHostedService>();

            services.AddOptions<DownstreamServicesOptions>()
               .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName));

            services.AddHttpClient<ICityEconomyBootstrapClient, CityEconomyBootstrapClient>((
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
                    identity: InternalServicePrincipals.SimulationCore,
                    EconomyPermissionKeys.EconomyBudgetBootstrap);

            services.AddHttpClient<ICityPopulationBootstrapClient, CityPopulationBootstrapClient>((
                    sp,
                    client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.Population))
                        throw new InvalidOperationException("DownstreamServices:Population is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.Population,
                        uriKind: UriKind.Absolute);
                })
               .AddInternalServiceAuthentication(
                    identity: InternalServicePrincipals.SimulationCore,
                    PopulationPermissionKeys.PopulationPeopleInitialize);

            services.AddHttpClient<ICityRoadSegmentConditionsClient, CityRoadSegmentConditionsClient>((
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
               .AddInternalServiceAuthentication(identity: InternalServicePrincipals.SimulationCore);

            return services;
        }
    }
}
