using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateIdentityDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.ApplyMigrationsIfEnabledAsync<IdentityDbContext>(
                services: services,
                serviceName: "Identity",
                cancellationToken: cancellationToken);
        }

        public static async Task SeedIdentityPermissionsAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            await DatabaseStartupRunner.RunSeedIfEnabledAsync(
                services: services,
                serviceName: "Identity",
                seedName: "IdentityPermissions",
                seedAction: async (
                    serviceProvider,
                    token) =>
                {
                    PermissionsSeeder permissionsSeeder = serviceProvider.GetRequiredService<PermissionsSeeder>();
                    await permissionsSeeder.SeedAsync(token);

                    RolesSeeder rolesSeeder = serviceProvider.GetRequiredService<RolesSeeder>();
                    await rolesSeeder.SeedSystemRolesAsync(token);

                    DefaultUserAccessPolicySeeder defaultUserAccessPolicySeeder =
                        serviceProvider.GetRequiredService<DefaultUserAccessPolicySeeder>();
                    await defaultUserAccessPolicySeeder.SeedAsync(token);

                    BootstrapSuperAdminSeeder bootstrapSuperAdminSeeder =
                        serviceProvider.GetRequiredService<BootstrapSuperAdminSeeder>();
                    await bootstrapSuperAdminSeeder.EnsureAtLeastOneSuperAdminAsync(token);
                },
                cancellationToken: cancellationToken);
        }
    }
}
