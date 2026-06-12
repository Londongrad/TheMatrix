using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Infrastructure.HostedServices;
using Matrix.SimulationCore.Infrastructure.Options;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Outbox.RabbitMq;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
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
            services.Replace(
                ServiceDescriptor.Singleton<ISimulationFixedStepSettings, SimulationTickFixedStepSettings>());
            services.TryAddSingleton(TimeProvider.System);

            services.AddRabbitMqOptions(configuration);
            services.AddMassTransitEndpointHygieneOptions(configuration);

            services.AddScoped<ISimulationClockRepository, SimulationClockRepository>();
            services.AddScoped<ISimulationInstanceRepository, SimulationInstanceRepository>();
            services.AddScoped<ISimulationHostReadRepository, SimulationHostReadRepository>();
            services.AddScoped<SimulationCoreOutboxWriter>();
            services.AddScoped<ISimulationCoreOutboxWriter>(sp =>
                sp.GetRequiredService<SimulationCoreOutboxWriter>());
            services.AddSingleton<IOutboxEventTypeContributor, SimulationCoreOutboxEventTypeContributor>();
            services.AddSingleton<OutboxEventTypeRegistry>();
            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<SimulationCoreDbContext>>();
            services.AddSingleton<SimulationOperationGate>();
            services.AddScoped<ISimulationBatchAdvanceExecutor, SimulationBatchAdvanceExecutor>();
            services.AddScoped<ISimulationClockMutationExecutor, SimulationClockMutationExecutor>();
            services.AddPermissionCheckingFromClaims();
            services.AddSingleton<IInternalServiceJwtIssuer, InternalServiceJwtIssuer>();

            services.AddOutbox<SimulationCoreDbContext>(configuration);
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();

            services.AddHostedService<SimulationTickHostedService>();

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
