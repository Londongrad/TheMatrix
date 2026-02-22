using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigratePopulationDatabaseAsync(
            this IApplicationBuilder app,
            CancellationToken cancellationToken = default)
        {
            await using AsyncServiceScope scope = app.ApplicationServices.CreateAsyncScope();

            PopulationDbContext dbContext = scope.ServiceProvider.GetRequiredService<PopulationDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
