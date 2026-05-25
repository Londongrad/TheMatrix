using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.Population.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigratePopulationDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<PopulationDbContext>(
                services: services,
                serviceName: "Population",
                cancellationToken: cancellationToken);
        }
    }
}
