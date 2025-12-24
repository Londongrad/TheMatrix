using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task SeedIdentityPermissionsAsync(
            this IApplicationBuilder app,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();

            PermissionsSeeder permissionsSeeder = scope.ServiceProvider.GetRequiredService<PermissionsSeeder>();
            await permissionsSeeder.SeedAsync(cancellationToken);

            RolesSeeder rolesSeeder = scope.ServiceProvider.GetRequiredService<RolesSeeder>();
            await rolesSeeder.SeedAdminRoleWithAllPermissionsAsync(cancellationToken);

            BootstrapAdminSeeder bootstrapAdminSeeder =
                scope.ServiceProvider.GetRequiredService<BootstrapAdminSeeder>();
            await bootstrapAdminSeeder.EnsureAtLeastOneAdminAsync(cancellationToken);
        }
    }
}
