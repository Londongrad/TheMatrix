using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Outbox;
using Matrix.Economy.Infrastructure.Outbox.RabbitMq;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity;
using Matrix.Economy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Matrix.Economy.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            string connectionString = configuration.GetConnectionString("EconomyDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'EconomyDb' is not configured.");
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            if (environment.IsDevelopment())
                connectionStringBuilder.IncludeErrorDetail = true;

            string effectiveConnectionString = connectionStringBuilder.ConnectionString;
            services.AddPostgresResilienceOptions(configuration);

            services.AddDbContext<EconomyDbContext>((sp, options) =>
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
            services.AddMassTransitEndpointHygieneOptions(configuration);

            services.AddScoped<ICityBudgetRepository, CityBudgetRepository>();
            services.AddScoped<ICityBudgetAllocationRepository, CityBudgetAllocationRepository>();
            services.AddScoped<ICityBudgetLedgerRepository, CityBudgetLedgerRepository>();
            services.AddScoped<ICityBudgetSettlementRepository, CityBudgetSettlementRepository>();
            services.AddScoped<ICityBusinessRepository, CityBusinessRepository>();
            services.AddScoped<ICityBusinessLedgerRepository, CityBusinessLedgerRepository>();
            services.AddScoped<ICityEconomyCostProfileStateRepository, CityEconomyCostProfileStateRepository>();
            services.AddScoped<ICityEconomyProgressionStateRepository, CityEconomyProgressionStateRepository>();
            services.AddScoped<ICityHouseholdAccountRepository, CityHouseholdAccountRepository>();
            services.AddScoped<ICityHouseholdAccountLedgerRepository, CityHouseholdAccountLedgerRepository>();
            services.AddScoped<ICityHouseholdObligationRepository, CityHouseholdObligationRepository>();
            services.AddScoped<ICityOperationalBudgetSignalPublisher, CityOperationalBudgetSignalOutboxWriter>();
            services.AddScoped<ICityPopulationSignalPublisher, CityPopulationSignalOutboxWriter>();
            services.AddScoped<ICityEconomyBootstrapService, CityEconomyBootstrapService>();
            services.AddScoped<IEconomyUnitOfWork, EconomyUnitOfWork>();
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<CityBudgetOperatingExpensePolicy>();
            services.AddPermissionCheckingFromClaims();
            services.AddOutbox<EconomyDbContext>(configuration);
            services.AddClassicCityScenarioInfrastructure();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();
                x.AddConsumer<CityCreatedConsumer, CityCreatedConsumerDefinition>();
                x.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
                x.AddConsumer<CityEconomyDailySettlementConsumer, CityEconomyDailySettlementConsumerDefinition>();
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
