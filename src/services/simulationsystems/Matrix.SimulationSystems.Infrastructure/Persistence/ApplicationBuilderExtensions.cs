using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateSimulationSystemsDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = services.CreateScope();
            SimulationSystemsDbContext dbContext = scope.ServiceProvider.GetRequiredService<SimulationSystemsDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
