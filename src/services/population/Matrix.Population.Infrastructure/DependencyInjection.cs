using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Infrastructure.Messaging;
using Matrix.Population.Infrastructure.Messaging.Cleanup;
using Matrix.Population.Infrastructure.Options;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.Population.Infrastructure.Outbox.RabbitMq;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure.SimulationCore;
using Matrix.Population.Infrastructure.SimulationSystems;
using SimulationCorePermissionKeys = Matrix.SimulationCore.Contracts.Authorization.Permissions.PermissionKeys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Matrix.Population.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("PopulationDb");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'PopulationDb' is not configured.");

            services.AddPostgresResilienceOptions(configuration);

            services.AddDbContext<PopulationDbContext>((
                sp,
                options) =>
            {
                PostgresResilienceOptions resilience = sp.GetRequiredService<IOptions<PostgresResilienceOptions>>()
                   .Value;

                options.UseNpgsql(
                    connectionString: connectionString,
                    npgsqlOptionsAction: npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: resilience.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(resilience.MaxRetryDelaySeconds),
                        errorCodesToAdd: null));
            });

            services.AddRabbitMqOptions(configuration);
            services.AddOptions<DownstreamServicesOptions>()
               .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName));
            services.AddMassTransitEndpointHygieneOptions(configuration);

            services.AddOptions<ProcessedIntegrationMessageCleanupOptions>()
               .Bind(configuration.GetSection(ProcessedIntegrationMessageCleanupOptions.SectionName));

            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IPersonReadRepository, PersonReadRepository>();
            services.AddScoped<IPersonWriteRepository, PersonWriteRepository>();
            services.AddClassicCityScenarioInfrastructure();
            services.AddScoped<IProcessedIntegrationMessageRepository, ProcessedIntegrationMessageRepository>();
            services.AddScoped<ProcessedIntegrationMessageCleaner>();
            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<PopulationDbContext>>();
            services.AddPermissionCheckingFromClaims();
            services.AddSingleton<IInternalServiceJwtIssuer, InternalServiceJwtIssuer>();
            services.AddHostedService<ProcessedIntegrationMessageCleanupHostedService>();
            services.AddOutbox<PopulationDbContext>(configuration);
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();
            services.AddScoped<ICityEconomySettlementOutboxWriter, CityEconomySettlementOutboxWriter>();
            services.AddHttpClient<ICityRouteResolutionClient, CityRouteResolutionClient>((sp, client) =>
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
            services.AddHttpClient<ICityPopulationActiveTripClient, CityActiveTripClient>((sp, client) =>
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
            services.AddHttpClient<ICityDistrictUtilityConditionsClient, CityDistrictUtilityConditionsClient>((sp, client) =>
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

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();
                x.AddClassicCityScenarioConsumers();

                x.UsingRabbitMq((
                    context,
                    cfg) =>
                {
                    RabbitMqOptions rmq = context.GetRequiredService<IOptions<RabbitMqOptions>>()
                       .Value;

                    cfg.Host(
                        host: rmq.Host,
                        port: rmq.Port,
                        virtualHost: rmq.VirtualHost,
                        configure: h =>
                        {
                            h.Username(rmq.Username);
                            h.Password(rmq.Password);
                        });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
