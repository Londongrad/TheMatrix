using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateCityCoreDatabaseAsync(
            this IApplicationBuilder app,
            CancellationToken cancellationToken = default)
        {
            await using AsyncServiceScope scope = app.ApplicationServices.CreateAsyncScope();

            CityCoreDbContext dbContext = scope.ServiceProvider.GetRequiredService<CityCoreDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
