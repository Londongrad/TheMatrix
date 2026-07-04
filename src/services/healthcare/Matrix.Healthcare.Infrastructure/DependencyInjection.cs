using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Outbox;
using Matrix.Healthcare.Infrastructure.Outbox.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Matrix.Healthcare.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddHealthcareInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment,
            Action<IBusRegistrationConfigurator>? configureConsumers = null)
        {
            string connectionString = configuration.GetConnectionString("HealthcareDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'HealthcareDb' is not configured.");
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            if (environment.IsDevelopment())
                connectionStringBuilder.IncludeErrorDetail = true;

            string effectiveConnectionString = connectionStringBuilder.ConnectionString;
            services.AddPostgresResilienceOptions(configuration);
            services.TryAddSingleton(TimeProvider.System);
            services.AddDbContext<HealthcareDbContext>((serviceProvider, options) =>
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

            services.AddScoped<IPatientProfileRepository, PatientProfileRepository>();
            services.AddScoped<IPatientMedicalRecordRepository, PatientMedicalRecordRepository>();
            services.AddScoped<IPatientCareNeedRepository, PatientCareNeedRepository>();
            services.AddScoped<IPatientCareAllocationRepository, PatientCareAllocationRepository>();
            services.AddScoped<IPatientCareAssignmentRepository, PatientCareAssignmentRepository>();
            services.AddScoped<IPatientHealthProgressionBatchSetRepository,
                PatientHealthProgressionBatchSetRepository>();
            services.AddScoped<ICareFacilityRepository, CareFacilityRepository>();
            services.AddScoped<IPatientHealthOutcomeOutboxWriter, PatientHealthOutcomeOutboxWriter>();
            services.AddScoped<IHealthcareSimulationDeletionRepository,
                HealthcareSimulationDeletionRepository>();
            services.AddScoped<IHealthcareUnitOfWork, HealthcareUnitOfWork>();
            services.AddOutbox<HealthcareDbContext>(configuration);
            services.AddScoped<IOutboxMessagePublisher, HealthcareMassTransitOutboxMessagePublisher>();

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
