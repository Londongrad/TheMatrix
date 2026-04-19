using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matrix.BuildingBlocks.Infrastructure.DatabaseStartup
{
    public static class DatabaseStartupRunner
    {
        public static async Task ApplyMigrationsIfEnabledAsync<TDbContext>(
            IServiceProvider services,
            string serviceName,
            CancellationToken cancellationToken = default)
            where TDbContext : DbContext
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();

            DatabaseStartupOptions options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseStartupOptions>>().Value;
            IHostEnvironment environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            ILogger logger = CreateLogger(scope.ServiceProvider, serviceName);

            if (!ShouldApplyMigrations(options, environment))
            {
                logger.LogInformation(
                    "Skipping automatic database migrations for {ServiceName} in {EnvironmentName}. Set {Section}:{Setting}=true to opt in.",
                    serviceName,
                    environment.EnvironmentName,
                    DatabaseStartupOptions.SectionName,
                    nameof(DatabaseStartupOptions.ApplyMigrationsOnStartup));
                return;
            }

            TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            string[] pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

            if (pendingMigrations.Length == 0)
            {
                logger.LogInformation("No pending database migrations for {ServiceName}.", serviceName);
                return;
            }

            HashSet<string> appliedBefore = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken))
               .ToHashSet(StringComparer.Ordinal);

            logger.LogInformation(
                "Applying {PendingMigrationCount} database migrations for {ServiceName}: {PendingMigrations}",
                pendingMigrations.Length,
                serviceName,
                pendingMigrations);

            await dbContext.Database.MigrateAsync(cancellationToken);

            string[] appliedAfter = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
            string[] newlyApplied = appliedAfter
               .Where(migration => !appliedBefore.Contains(migration))
               .ToArray();

            logger.LogInformation(
                "Applied {AppliedMigrationCount} database migrations for {ServiceName}: {AppliedMigrations}",
                newlyApplied.Length,
                serviceName,
                newlyApplied.Length > 0 ? newlyApplied : pendingMigrations);
        }

        public static async Task RunSeedIfEnabledAsync(
            IServiceProvider services,
            string serviceName,
            string seedName,
            Func<IServiceProvider, CancellationToken, Task> seedAction,
            CancellationToken cancellationToken = default)
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();

            DatabaseStartupOptions options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseStartupOptions>>().Value;
            IHostEnvironment environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            ILogger logger = CreateLogger(scope.ServiceProvider, serviceName);

            if (!ShouldRunSeed(options, environment))
            {
                logger.LogInformation(
                    "Skipping automatic seed {SeedName} for {ServiceName} in {EnvironmentName}. Set {Section}:{Setting}=true to opt in.",
                    seedName,
                    serviceName,
                    environment.EnvironmentName,
                    DatabaseStartupOptions.SectionName,
                    nameof(DatabaseStartupOptions.RunSeedOnStartup));
                return;
            }

            logger.LogInformation("Running startup seed {SeedName} for {ServiceName}.", seedName, serviceName);
            await seedAction(scope.ServiceProvider, cancellationToken);
            logger.LogInformation("Completed startup seed {SeedName} for {ServiceName}.", seedName, serviceName);
        }

        private static ILogger CreateLogger(IServiceProvider services, string serviceName)
        {
            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"Matrix.DatabaseStartup.{serviceName}");
        }

        private static bool ShouldApplyMigrations(DatabaseStartupOptions options, IHostEnvironment environment)
        {
            return options.ApplyMigrationsOnStartup ?? environment.IsDevelopment();
        }

        private static bool ShouldRunSeed(DatabaseStartupOptions options, IHostEnvironment environment)
        {
            return options.RunSeedOnStartup ?? environment.IsDevelopment();
        }
    }
}
