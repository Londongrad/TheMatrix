using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Matrix.Education.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEducationInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment,
            Action<IBusRegistrationConfigurator>? configureConsumers = null)
        {
            string connectionString = configuration.GetConnectionString("EducationDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'EducationDb' is not configured.");
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            if (environment.IsDevelopment())
                connectionStringBuilder.IncludeErrorDetail = true;

            string effectiveConnectionString = connectionStringBuilder.ConnectionString;
            services.AddPostgresResilienceOptions(configuration);
            services.TryAddSingleton(TimeProvider.System);
            services.AddDbContext<EducationDbContext>((serviceProvider, options) =>
            {
                PostgresResilienceOptions resilience = serviceProvider
                   .GetRequiredService<IOptions<PostgresResilienceOptions>>()
                   .Value;

                options.UseNpgsql(
                    connectionString: effectiveConnectionString,
                    npgsqlOptionsAction: npgsql => npgsql.EnableRetryOnFailure(
                        maxRetryCount: resilience.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(resilience.MaxRetryDelaySeconds),
                        errorCodesToAdd: null));

                if (environment.IsDevelopment())
                    options.EnableDetailedErrors();
            });

            services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
            services.AddScoped<IEducationInstitutionRepository, EducationInstitutionRepository>();
            services.AddScoped<IStudentEnrollmentRepository, StudentEnrollmentRepository>();
            services.AddScoped<IEducationProgressionCheckpointRepository,
                EducationProgressionCheckpointRepository>();
            services.AddScoped<IEducationSimulationDeletionRepository,
                EducationSimulationDeletionRepository>();
            services.AddScoped<IEducationUnitOfWork, EducationUnitOfWork>();

            services.AddRabbitMqOptions(configuration);
            services.AddMassTransitEndpointHygieneOptions(configuration);
            services.AddMassTransit(registration =>
            {
                registration.SetKebabCaseEndpointNameFormatter();
                registration.AddRabbitMqEndpointHygiene();
                configureConsumers?.Invoke(registration);

                registration.UsingRabbitMq((context, bus) =>
                {
                    RabbitMqOptions rabbitMq = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                    bus.Host(
                        host: rabbitMq.Host,
                        port: rabbitMq.Port,
                        virtualHost: rabbitMq.VirtualHost,
                        configure: host =>
                        {
                            host.Username(rabbitMq.Username);
                            host.Password(rabbitMq.Password);
                        });

                    bus.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
