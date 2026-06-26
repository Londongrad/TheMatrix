using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.Healthcare.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static Task MigrateHealthcareDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            return DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<HealthcareDbContext>(
                services: services,
                serviceName: "Healthcare",
                cancellationToken: cancellationToken);
        }
    }
}
