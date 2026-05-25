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

            DatabaseStartupOptions options = scope.ServiceProvider
               .GetRequiredService<IOptions<DatabaseStartupOptions>>()
               .Value;
            IHostEnvironment environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            ILogger logger = CreateLogger(
                services: scope.ServiceProvider,
                serviceName: serviceName);

            if (!ShouldApplyMigrations(
                    options: options,
                    environment: environment))
            {
                logger.LogInformation(
                    message:
                    "Skipping automatic database migrations for {ServiceName} in {EnvironmentName}. Set {Section}:{Setting}=true to opt in.",
                    serviceName,
                    environment.EnvironmentName,
                    DatabaseStartupOptions.SectionName,
                    nameof(DatabaseStartupOptions.ApplyMigrationsOnStartup));
                return;
            }

            TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            await DatabaseMigrationExecutor.ApplyMigrationsAsync(
                dbContext: dbContext,
                logger: logger,
                serviceName: serviceName,
                cancellationToken: cancellationToken);
        }

        public static async Task RunSeedIfEnabledAsync(
            IServiceProvider services,
            string serviceName,
            string seedName,
            Func<IServiceProvider, CancellationToken, Task> seedAction,
            CancellationToken cancellationToken = default)
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();

            DatabaseStartupOptions options = scope.ServiceProvider
               .GetRequiredService<IOptions<DatabaseStartupOptions>>()
               .Value;
            IHostEnvironment environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            ILogger logger = CreateLogger(
                services: scope.ServiceProvider,
                serviceName: serviceName);

            if (!ShouldRunSeed(
                    options: options,
                    environment: environment))
            {
                logger.LogInformation(
                    message:
                    "Skipping automatic seed {SeedName} for {ServiceName} in {EnvironmentName}. Set {Section}:{Setting}=true to opt in.",
                    seedName,
                    serviceName,
                    environment.EnvironmentName,
                    DatabaseStartupOptions.SectionName,
                    nameof(DatabaseStartupOptions.RunSeedOnStartup));
                return;
            }

            logger.LogInformation(
                message: "Running startup seed {SeedName} for {ServiceName}.",
                seedName,
                serviceName);
            await seedAction(
                arg1: scope.ServiceProvider,
                arg2: cancellationToken);
            logger.LogInformation(
                message: "Completed startup seed {SeedName} for {ServiceName}.",
                seedName,
                serviceName);
        }

        private static ILogger CreateLogger(
            IServiceProvider services,
            string serviceName)
        {
            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"Matrix.DatabaseStartup.{serviceName}");
        }

        private static bool ShouldApplyMigrations(
            DatabaseStartupOptions options,
            IHostEnvironment environment)
        {
            return options.ApplyMigrationsOnStartup ?? environment.IsDevelopment();
        }

        private static bool ShouldRunSeed(
            DatabaseStartupOptions options,
            IHostEnvironment environment)
        {
            return options.RunSeedOnStartup ?? environment.IsDevelopment();
        }
    }
}
