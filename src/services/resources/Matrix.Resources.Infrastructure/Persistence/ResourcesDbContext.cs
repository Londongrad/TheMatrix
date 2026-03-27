using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public sealed class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options)
        : DbContext(options)
    {
        public DbSet<CityStockpileState> CityStockpiles => Set<CityStockpileState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourcesDbContext).Assembly);
        }
    }
}
