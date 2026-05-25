using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateSimulationCoreDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<SimulationCoreDbContext>(
                services: services,
                serviceName: "SimulationCore",
                cancellationToken: cancellationToken);
        }
    }
}
