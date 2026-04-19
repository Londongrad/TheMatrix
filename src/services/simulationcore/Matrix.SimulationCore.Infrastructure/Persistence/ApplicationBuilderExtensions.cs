using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateSimulationCoreDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<SimulationCoreDbContext>(
                services,
                serviceName: "SimulationCore",
                cancellationToken);
        }
    }
}
