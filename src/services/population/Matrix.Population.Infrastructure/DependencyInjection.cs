using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Messaging.Cleanup;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Infrastructure.Messaging;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity;
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
            string connectionString = configuration.GetConnectionString("PopulationDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'PopulationDb' is not configured.");

            services.AddDbContext<PopulationDbContext>(options => { options.UseNpgsql(connectionString); });

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
            services.AddHostedService<ProcessedIntegrationMessageCleanupHostedService>();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
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
