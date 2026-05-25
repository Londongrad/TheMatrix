using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matrix.BuildingBlocks.Infrastructure.DatabaseStartup
{
    public static class DatabaseMigrationExecutor
    {
        public static async Task ApplyMigrationsAsync<TDbContext>(
            TDbContext dbContext,
            ILogger logger,
            string serviceName,
            CancellationToken cancellationToken = default)
            where TDbContext : DbContext
        {
            string[] pendingMigrations =
                (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

            if (pendingMigrations.Length == 0)
            {
                logger.LogInformation(
                    message: "No pending database migrations for {ServiceName}.",
                    serviceName);
                return;
            }

            var appliedBefore = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken))
               .ToHashSet(StringComparer.Ordinal);

            logger.LogInformation(
                message: "Applying {PendingMigrationCount} database migrations for {ServiceName}: {PendingMigrations}",
                pendingMigrations.Length,
                serviceName,
                pendingMigrations);

            await dbContext.Database.MigrateAsync(cancellationToken);

            string[] appliedAfter = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
            string[] newlyApplied = appliedAfter
               .Where(migration => !appliedBefore.Contains(migration))
               .ToArray();

            logger.LogInformation(
                message: "Applied {AppliedMigrationCount} database migrations for {ServiceName}: {AppliedMigrations}",
                newlyApplied.Length,
                serviceName,
                newlyApplied.Length > 0
                    ? newlyApplied
                    : pendingMigrations);
        }
    }
}
