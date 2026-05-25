using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateEconomyDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<EconomyDbContext>(
                services: services,
                serviceName: "Economy",
                cancellationToken: cancellationToken);
        }
    }
}
