using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateSimulationCoreDatabaseAsync(
            this IApplicationBuilder app,
            CancellationToken cancellationToken = default)
        {
            await using AsyncServiceScope scope = app.ApplicationServices.CreateAsyncScope();

            SimulationCoreDbContext dbContext = scope.ServiceProvider.GetRequiredService<SimulationCoreDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
