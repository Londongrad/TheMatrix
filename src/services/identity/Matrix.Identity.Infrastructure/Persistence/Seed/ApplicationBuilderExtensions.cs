using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task SeedIdentityPermissionsAsync(
            this IApplicationBuilder app,
            CancellationToken ct = default)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();

            PermissionsSeeder permissionsSeeder = scope.ServiceProvider.GetRequiredService<PermissionsSeeder>();
            await permissionsSeeder.SeedAsync(ct);

            RolesSeeder rolesSeeder = scope.ServiceProvider.GetRequiredService<RolesSeeder>();
            await rolesSeeder.SeedAdminRoleWithAllPermissionsAsync(ct);

            BootstrapAdminSeeder bootstrapAdminSeeder =
                scope.ServiceProvider.GetRequiredService<BootstrapAdminSeeder>();
            await bootstrapAdminSeeder.EnsureAtLeastOneAdminAsync(ct);
        }
    }
}
