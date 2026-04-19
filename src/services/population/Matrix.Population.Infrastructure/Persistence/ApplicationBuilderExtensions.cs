using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigratePopulationDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<PopulationDbContext>(
                services,
                serviceName: "Population",
                cancellationToken);
        }
    }
}
