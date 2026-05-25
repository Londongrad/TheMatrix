using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Matrix.DatabaseMigrationRunner
{
    internal static class MigrationRunnerExecutor
    {
        public static async Task ApplyAsync<TDbContext>(
            string connectionString,
            ILogger logger,
            IHostEnvironment environment,
            string serviceName,
            CancellationToken cancellationToken = default)
            where TDbContext : DbContext
        {
            DbContextOptions<TDbContext> dbContextOptions = BuildDbContextOptions<TDbContext>(
                connectionString: connectionString,
                environment: environment);
            await using TDbContext dbContext = CreateDbContext<TDbContext>(dbContextOptions);

            await DatabaseMigrationExecutor.ApplyMigrationsAsync(
                dbContext: dbContext,
                logger: logger,
                serviceName: serviceName,
                cancellationToken: cancellationToken);
        }

        private static DbContextOptions<TDbContext> BuildDbContextOptions<TDbContext>(
            string connectionString,
            IHostEnvironment environment)
            where TDbContext : DbContext
        {
            DbContextOptionsBuilder<TDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);

            if (environment.IsDevelopment())
                optionsBuilder.EnableDetailedErrors();

            return optionsBuilder.Options;
        }

        private static TDbContext CreateDbContext<TDbContext>(DbContextOptions<TDbContext> options)
            where TDbContext : DbContext
        {
            object? dbContext = Activator.CreateInstance(
                type: typeof(TDbContext),
                options);

            return dbContext as TDbContext ??
                   throw new InvalidOperationException($"Failed to construct DbContext {typeof(TDbContext).Name}.");
        }
    }
}
