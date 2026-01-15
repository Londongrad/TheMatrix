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
               .Bind(configuration.GetSection(OutboxOptions.SectionName));

            services.TryAddSingleton(TimeProvider.System);

            services.AddScoped<IOutboxRepository, PostgresOutboxRepository<TDbContext>>();
            services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
            services.AddHostedService<OutboxDispatcherHostedService>();

            return services;
        }
    }
}
