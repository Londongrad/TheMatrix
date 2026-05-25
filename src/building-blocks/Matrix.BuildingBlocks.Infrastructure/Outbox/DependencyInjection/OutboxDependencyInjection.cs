using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Dispatching;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Options;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection
{
    public static class OutboxDependencyInjection
    {
        public static IServiceCollection AddOutbox<TDbContext>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TDbContext : DbContext
        {
            services.AddOptions<OutboxOptions>()
               .Bind(configuration.GetSection(OutboxOptions.SectionName))
               .Validate(
                    validation: o => o.BatchSize > 0,
                    failureMessage: $"{OutboxOptions.SectionName}:BatchSize must be greater than 0.")
               .Validate(
                    validation: o => o.PollIntervalSeconds > 0,
                    failureMessage: $"{OutboxOptions.SectionName}:PollIntervalSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.LeaseTtlSeconds > 0,
                    failureMessage: $"{OutboxOptions.SectionName}:LeaseTtlSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.FailureBackoffMaxSeconds > 0,
                    failureMessage: $"{OutboxOptions.SectionName}:FailureBackoffMaxSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.ProcessedRetentionSeconds >= 0,
                    failureMessage:
                    $"{OutboxOptions.SectionName}:ProcessedRetentionSeconds must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.CleanupBatchSize >= 0,
                    failureMessage: $"{OutboxOptions.SectionName}:CleanupBatchSize must be greater than or equal to 0.")
               .ValidateOnStart();

            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IOutboxRepository, PostgresOutboxRepository<TDbContext>>();
            services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
            services.AddHostedService<OutboxDispatcherHostedService>();

            return services;
        }
    }
}
