using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.Resources.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using EconomyPermissionKeys = Matrix.Economy.Contracts.Authorization.Permissions.PermissionKeys;
using Matrix.Resources.Infrastructure.Economy;
using Matrix.Resources.Infrastructure.Options;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Persistence.Repositories;
using Matrix.Resources.Infrastructure.Outbox;
using Matrix.Resources.Infrastructure.Outbox.RabbitMq;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity;
using Matrix.Resources.Infrastructure.SimulationCore;
using SimulationCorePermissionKeys = Matrix.SimulationCore.Contracts.Authorization.Permissions.PermissionKeys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Matrix.Resources.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            Guid resourcesServicePrincipalId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            string connectionString = configuration.GetConnectionString("ResourcesDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'ResourcesDb' is not configured.");
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            if (environment.IsDevelopment())
                connectionStringBuilder.IncludeErrorDetail = true;

            string effectiveConnectionString = connectionStringBuilder.ConnectionString;
            services.AddPostgresResilienceOptions(configuration);

            services.AddDbContext<ResourcesDbContext>((sp, options) =>
            {
                PostgresResilienceOptions resilience = sp.GetRequiredService<IOptions<PostgresResilienceOptions>>()
                   .Value;

                options.UseNpgsql(
                    connectionString: effectiveConnectionString,
                    npgsqlOptionsAction: npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: resilience.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(resilience.MaxRetryDelaySeconds),
                        errorCodesToAdd: null));

                if (environment.IsDevelopment())
                    options.EnableDetailedErrors();
            });

            services.AddOptions<RabbitMqOptions>()
               .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Host),
                    failureMessage: "RabbitMq:Host is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Username),
                    failureMessage: "RabbitMq:Username is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Password),
                    failureMessage: "RabbitMq:Password is required.")
               .ValidateOnStart();
            services.AddOptions<DownstreamServicesOptions>()
               .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName));
            services.AddMassTransitEndpointHygieneOptions(configuration);

            services.AddScoped<ICityStockpileRepository, CityStockpileRepository>();
            services.AddClassicCityScenarioInfrastructure();
            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<ResourcesDbContext>>();
            services.AddPermissionCheckingFromClaims();
            services.AddSingleton<IInternalServiceJwtIssuer, InternalServiceJwtIssuer>();
            services.AddOutbox<ResourcesDbContext>(configuration);
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();
            services.AddScoped<ICityStockpileSnapshotOutboxWriter, CityStockpileSnapshotOutboxWriter>();
            services.AddScoped<ICityOperationalExpenseOutboxWriter, CityOperationalExpenseOutboxWriter>();
            services.AddHttpClient<ICityBudgetAuthorizationClient, CityBudgetAuthorizationClient>((sp, client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.Economy))
                        throw new InvalidOperationException("DownstreamServices:Economy is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.Economy,
                        uriKind: UriKind.Absolute);
                })
               .AddHttpMessageHandler(sp => new InternalScopedServiceAuthenticationHandler(
                    jwtIssuer: sp.GetRequiredService<IInternalServiceJwtIssuer>(),
                    subjectId: resourcesServicePrincipalId,
                    serviceName: "resources",
                    permissions:
                    [
                        EconomyPermissionKeys.EconomyBudgetAuthorize
                    ]));
            services.AddHttpClient<ICityResupplyTripDispatcher, CityResupplyTripDispatcher>((sp, client) =>
                {
                    DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
                       .Value;

                    if (string.IsNullOrWhiteSpace(options.SimulationCore))
                        throw new InvalidOperationException("DownstreamServices:SimulationCore is not configured.");

                    client.BaseAddress = new Uri(
                        uriString: options.SimulationCore,
                        uriKind: UriKind.Absolute);
                })
               .AddHttpMessageHandler(sp => new InternalScopedServiceAuthenticationHandler(
                    jwtIssuer: sp.GetRequiredService<IInternalServiceJwtIssuer>(),
                    subjectId: resourcesServicePrincipalId,
                    serviceName: "resources",
                    permissions:
                    [
                        SimulationCorePermissionKeys.SimulationCoreClassicCityRead,
                        SimulationCorePermissionKeys.SimulationCoreClassicCityUpdate
                    ]));

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();
                x.AddClassicCityScenarioConsumers();

                x.UsingRabbitMq((context, cfg) =>
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
