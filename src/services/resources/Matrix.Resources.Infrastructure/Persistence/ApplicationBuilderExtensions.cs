using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateResourcesDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<ResourcesDbContext>(
                services: services,
                serviceName: "Resources",
                cancellationToken: cancellationToken);
        }
    }
}
