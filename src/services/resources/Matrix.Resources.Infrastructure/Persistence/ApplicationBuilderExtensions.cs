using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateResourcesDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<ResourcesDbContext>(
                services,
                serviceName: "Resources",
                cancellationToken);
        }
    }
}
