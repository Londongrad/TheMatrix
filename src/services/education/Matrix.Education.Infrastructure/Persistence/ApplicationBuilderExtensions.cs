using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;

namespace Matrix.Education.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static Task MigrateEducationDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            return DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<EducationDbContext>(
                services: services,
                serviceName: "Education",
                cancellationToken: cancellationToken);
        }
    }
}
