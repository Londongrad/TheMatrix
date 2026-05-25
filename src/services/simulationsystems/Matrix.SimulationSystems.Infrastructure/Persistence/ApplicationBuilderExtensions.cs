using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateSimulationSystemsDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<SimulationSystemsDbContext>(
                services: services,
                serviceName: "SimulationSystems",
                cancellationToken: cancellationToken);
        }
    }
}
