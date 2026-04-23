using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using EconomyPermissionKeys = Matrix.Economy.Contracts.Authorization.Permissions.PermissionKeys;
using PopulationPermissionKeys = Matrix.Population.Contracts.Authorization.Permissions.PermissionKeys;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Infrastructure.Economy;
using Matrix.SimulationCore.Infrastructure.Options;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Population;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Infrastructure.HostedServices;
using Matrix.SimulationCore.Infrastructure.Outbox.RabbitMq;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
using Matrix.SimulationCore.Infrastructure.SimulationSystems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Matrix.SimulationCore.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("SimulationCoreDb");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'SimulationCoreDb' is not configured.");

            services.AddPostgresResilienceOptions(configuration);

            services.AddDbContext<SimulationCoreDbContext>((
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

            services.AddOptions<SimulationTickOptions>()
               .Bind(configuration.GetSection(SimulationTickOptions.SectionName));
            services.AddOptions<ProvisioningRecoveryOptions>()
               .Bind(configuration.GetSection(ProvisioningRecoveryOptions.SectionName));
            services.TryAddSingleton(TimeProvider.System);

            services.AddRabbitMqOptions(configuration);
            services.AddOptions<DownstreamServicesOptions>()
               .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName));
            services.AddMassTransitEndpointHygieneOptions(configuration);

            services.AddScoped<ISimulationClockRepository, SimulationClockRepository>();
            services.AddClassicCityScenarioInfrastructure();
            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<SimulationCoreDbContext>>();
            services.AddSingleton<SimulationOperationGate>();
            services.AddScoped<ISimulationBatchAdvanceExecutor, SimulationBatchAdvanceExecutor>();
            services.AddScoped<ISimulationClockMutationExecutor, SimulationClockMutationExecutor>();
            services.AddPermissionCheckingFromClaims();
            services.AddSingleton<IInternalServiceJwtIssuer, InternalServiceJwtIssuer>();

            services.AddOutbox<SimulationCoreDbContext>(configuration);
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();

            services.AddHttpClient<ICityEconomyBootstrapClient, CityEconomyBootstrapClient>((sp, client) =>
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

            services.AddHttpClient<ICityPopulationBootstrapClient, CityPopulationBootstrapClient>((sp, client) =>
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

            services.AddHttpClient<ICityRoadSegmentConditionsClient, CityRoadSegmentConditionsClient>((sp, client) =>
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

            services.AddHostedService<SimulationTickHostedService>();
            services.AddHostedService<CityProvisioningHostedService>();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();

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
