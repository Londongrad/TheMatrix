using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public sealed class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options)
        : DbContext(options)
    {
        public DbSet<CityStockpileState> CityStockpiles => Set<CityStockpileState>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourcesDbContext).Assembly);
        }
    }
}
