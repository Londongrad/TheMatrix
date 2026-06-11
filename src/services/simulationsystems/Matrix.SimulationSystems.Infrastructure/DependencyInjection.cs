using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Options;
using Matrix.SimulationSystems.Infrastructure.Outbox;
using Matrix.SimulationSystems.Infrastructure.Outbox.RabbitMq;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Matrix.SimulationSystems.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment,
            Action<IBusRegistrationConfigurator>? configureConsumers = null)
        {
            string connectionString = configuration.GetConnectionString("SimulationSystemsDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'SimulationSystemsDb' is not configured.");
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            if (environment.IsDevelopment())
                connectionStringBuilder.IncludeErrorDetail = true;

            string effectiveConnectionString = connectionStringBuilder.ConnectionString;
            services.AddPostgresResilienceOptions(configuration);
            services.TryAddSingleton(TimeProvider.System);

            services.AddDbContext<SimulationSystemsDbContext>((
                sp,
                options) =>
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

            services.AddRabbitMqOptions(configuration);
            services.AddOptions<DownstreamServicesOptions>()
               .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName));
            services.AddMassTransitEndpointHygieneOptions(configuration);

            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<SimulationSystemsDbContext>>();
            services.AddPermissionCheckingFromClaims();
            services.AddSingleton<IInternalServiceJwtIssuer, InternalServiceJwtIssuer>();
            services.AddOutbox<SimulationSystemsDbContext>(configuration);
            services.AddSingleton<OutboxEventTypeRegistry>();
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();
                configureConsumers?.Invoke(x);

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
