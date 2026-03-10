using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateEconomyDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = services.CreateScope();
            EconomyDbContext dbContext = scope.ServiceProvider.GetRequiredService<EconomyDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
