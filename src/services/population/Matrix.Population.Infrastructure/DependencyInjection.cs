using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Infrastructure.Messaging;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddScoped<IPersonReadRepository, PersonReadRepository>();
            services.AddScoped<IPersonWriteRepository, PersonWriteRepository>();
            services.AddScoped<ICityPopulationPersonReadRepository, CityPopulationPersonReadRepository>();
            services.AddScoped<IHouseholdWriteRepository, HouseholdWriteRepository>();
            services.AddScoped<ICityPopulationArchiveStateRepository, CityPopulationArchiveStateRepository>();
            services.AddScoped<ICityPopulationDeletionStateRepository, CityPopulationDeletionStateRepository>();
            services.AddScoped<ICityPopulationEnvironmentRepository, CityPopulationEnvironmentRepository>();
            services.AddScoped<ICityPopulationProgressionStateRepository, CityPopulationProgressionStateRepository>();
            services.AddScoped<ICityPopulationSummaryReadRepository, CityPopulationSummaryReadRepository>();
            services
               .AddScoped<ICityPopulationWeatherImpactStateRepository, CityPopulationWeatherImpactStateRepository>();
            services
               .AddScoped<ICityPopulationWeatherExposureStateRepository,
                    CityPopulationWeatherExposureStateRepository>();
            services.AddScoped<IProcessedIntegrationMessageRepository, ProcessedIntegrationMessageRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddPermissionCheckingFromClaims();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<CityArchivedConsumer, CityArchivedConsumerDefinition>();
                x.AddConsumer<CityDeletedConsumer, CityDeletedConsumerDefinition>();
                x.AddConsumer<CityEnvironmentChangedConsumer, CityEnvironmentChangedConsumerDefinition>();
                x.AddConsumer<CityTimeAdvancedConsumer, CityTimeAdvancedConsumerDefinition>();
                x.AddConsumer<CityWeatherCreatedConsumer, CityWeatherCreatedConsumerDefinition>();
                x.AddConsumer<CityWeatherChangedConsumer, CityWeatherChangedConsumerDefinition>();

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
