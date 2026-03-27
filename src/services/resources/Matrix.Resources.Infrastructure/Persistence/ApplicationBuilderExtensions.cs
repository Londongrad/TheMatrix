using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static async Task MigrateResourcesDatabaseAsync(
            this IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = services.CreateScope();
            ResourcesDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
