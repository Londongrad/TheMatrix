using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateEconomyDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<EconomyDbContext>(
                services,
                serviceName: "Economy",
                cancellationToken);
        }
    }
}
