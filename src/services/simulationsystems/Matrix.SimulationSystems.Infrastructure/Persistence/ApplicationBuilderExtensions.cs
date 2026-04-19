using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateSimulationSystemsDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<SimulationSystemsDbContext>(
                services,
                serviceName: "SimulationSystems",
                cancellationToken);
        }
    }
}
