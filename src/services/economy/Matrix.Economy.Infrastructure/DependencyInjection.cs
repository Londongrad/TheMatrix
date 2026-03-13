using MassTransit;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Messaging;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Matrix.Economy.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("EconomyDb")
                ?? throw new InvalidOperationException("Connection string 'EconomyDb' is not configured.");

            services.AddDbContext<EconomyDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
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

            services.AddScoped<ICityBudgetRepository, CityBudgetRepository>();
            services.AddScoped<ICityBudgetAllocationRepository, CityBudgetAllocationRepository>();
            services.AddScoped<ICityBudgetLedgerRepository, CityBudgetLedgerRepository>();
            services.AddScoped<ICityBudgetSettlementRepository, CityBudgetSettlementRepository>();
            services.AddScoped<ICityBusinessRepository, CityBusinessRepository>();
            services.AddScoped<ICityBusinessLedgerRepository, CityBusinessLedgerRepository>();
            services.AddScoped<ICityHouseholdAccountRepository, CityHouseholdAccountRepository>();
            services.AddScoped<ICityHouseholdAccountLedgerRepository, CityHouseholdAccountLedgerRepository>();
            services.AddScoped<ICityHouseholdObligationRepository, CityHouseholdObligationRepository>();
            services.AddScoped<IEconomyUnitOfWork, EconomyUnitOfWork>();
            services.AddSingleton<CityBudgetOperatingExpensePolicy>();
            services.AddSingleton<CityEconomySimulationTemplatePolicy>();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<CityCreatedConsumer, CityCreatedConsumerDefinition>();
                x.AddConsumer<CityEconomyDailySettlementConsumer, CityEconomyDailySettlementConsumerDefinition>();
                x.AddConsumer<ClassicCityHouseholdAccountSyncConsumer, ClassicCityHouseholdAccountSyncConsumerDefinition>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    RabbitMqOptions rmq = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

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
